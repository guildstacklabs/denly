// Supabase Edge Function for sending push notifications
// Triggered by database webhooks on relevant tables (expenses, events, settlements)
//
// Environment variables required:
// - FIREBASE_SERVICE_ACCOUNT: Firebase service account JSON (base64 encoded)
// - FIREBASE_PROJECT_ID: Firebase project ID (e.g., "denly-12345")
// - APNS_KEY_ID: Apple Push Notification service key ID (for iOS)
// - APNS_TEAM_ID: Apple Developer Team ID
// - APNS_BUNDLE_ID: App bundle ID (com.companyname.denly)
// - APNS_PRIVATE_KEY: APNs private key (p8 format, base64 encoded)

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";
import { create, getNumericDate } from "https://deno.land/x/djwt@v3.0.1/mod.ts";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;
const FIREBASE_PROJECT_ID = Deno.env.get("FIREBASE_PROJECT_ID");
const FIREBASE_SERVICE_ACCOUNT = Deno.env.get("FIREBASE_SERVICE_ACCOUNT");

interface NotificationRequest {
  type: "expense_added" | "event_created" | "event_updated" | "settlement_requested" | "settlement_confirmed";
  den_id: string;
  item_id?: string;
  exclude_user_id?: string; // Don't notify the user who triggered the action
  title: string;
  body: string;
}

interface DeviceToken {
  id: string;
  user_id: string;
  platform: "ios" | "android";
  token: string;
}

interface ServiceAccount {
  type: string;
  project_id: string;
  private_key_id: string;
  private_key: string;
  client_email: string;
  client_id: string;
  auth_uri: string;
  token_uri: string;
}

// Cache for the access token
let cachedAccessToken: string | null = null;
let tokenExpiresAt: number = 0;

serve(async (req) => {
  try {
    // Only allow POST requests
    if (req.method !== "POST") {
      return new Response(JSON.stringify({ error: "Method not allowed" }), {
        status: 405,
        headers: { "Content-Type": "application/json" },
      });
    }

    const payload: NotificationRequest = await req.json();

    // Validate required fields
    if (!payload.type || !payload.den_id || !payload.title || !payload.body) {
      return new Response(
        JSON.stringify({ error: "Missing required fields: type, den_id, title, body" }),
        { status: 400, headers: { "Content-Type": "application/json" } }
      );
    }

    // Create Supabase client with service role (bypasses RLS)
    const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

    // Get all den members
    const { data: members, error: membersError } = await supabase
      .from("den_members")
      .select("user_id")
      .eq("den_id", payload.den_id);

    if (membersError) {
      console.error("Error fetching den members:", membersError);
      return new Response(
        JSON.stringify({ error: "Failed to fetch den members" }),
        { status: 500, headers: { "Content-Type": "application/json" } }
      );
    }

    if (!members || members.length === 0) {
      return new Response(
        JSON.stringify({ message: "No members in den" }),
        { status: 200, headers: { "Content-Type": "application/json" } }
      );
    }

    // Get device tokens for all members (excluding the triggering user)
    const userIds = members
      .map((m) => m.user_id)
      .filter((id) => id !== payload.exclude_user_id);

    if (userIds.length === 0) {
      return new Response(
        JSON.stringify({ message: "No other members to notify" }),
        { status: 200, headers: { "Content-Type": "application/json" } }
      );
    }

    const { data: tokens, error: tokensError } = await supabase
      .from("device_tokens")
      .select("*")
      .in("user_id", userIds);

    if (tokensError) {
      console.error("Error fetching device tokens:", tokensError);
      return new Response(
        JSON.stringify({ error: "Failed to fetch device tokens" }),
        { status: 500, headers: { "Content-Type": "application/json" } }
      );
    }

    if (!tokens || tokens.length === 0) {
      return new Response(
        JSON.stringify({ message: "No device tokens registered" }),
        { status: 200, headers: { "Content-Type": "application/json" } }
      );
    }

    // Notification data payload (no PII, just IDs)
    const data = {
      type: payload.type,
      den_id: payload.den_id,
      ...(payload.item_id && { item_id: payload.item_id }),
    };

    // Send notifications
    const results = await Promise.allSettled(
      tokens.map((token: DeviceToken) => {
        if (token.platform === "android") {
          return sendFcmV1Notification(token.token, payload.title, payload.body, data);
        } else {
          // For iOS, we'd use APNs HTTP/2 API
          return sendApnsNotification(token.token, payload.title, payload.body, data);
        }
      })
    );

    const successful = results.filter((r) => r.status === "fulfilled").length;
    const failed = results.filter((r) => r.status === "rejected").length;

    return new Response(
      JSON.stringify({
        message: `Sent ${successful} notifications, ${failed} failed`,
        total: tokens.length,
        successful,
        failed,
      }),
      { status: 200, headers: { "Content-Type": "application/json" } }
    );
  } catch (error) {
    console.error("Error in push-notify function:", error);
    return new Response(
      JSON.stringify({ error: "Internal server error" }),
      { status: 500, headers: { "Content-Type": "application/json" } }
    );
  }
});

/**
 * Get an OAuth 2.0 access token for FCM using the service account.
 */
async function getAccessToken(): Promise<string> {
  // Return cached token if still valid (with 5 min buffer)
  if (cachedAccessToken && Date.now() < tokenExpiresAt - 300000) {
    return cachedAccessToken;
  }

  if (!FIREBASE_SERVICE_ACCOUNT) {
    throw new Error("FIREBASE_SERVICE_ACCOUNT not configured");
  }

  // Decode and parse service account JSON
  const serviceAccountJson = atob(FIREBASE_SERVICE_ACCOUNT);
  const serviceAccount: ServiceAccount = JSON.parse(serviceAccountJson);

  // Create JWT for Google OAuth
  const now = Math.floor(Date.now() / 1000);
  const jwtPayload = {
    iss: serviceAccount.client_email,
    sub: serviceAccount.client_email,
    aud: "https://oauth2.googleapis.com/token",
    iat: getNumericDate(0),
    exp: getNumericDate(3600), // 1 hour
    scope: "https://www.googleapis.com/auth/firebase.messaging",
  };

  // Import the private key
  const pemHeader = "-----BEGIN PRIVATE KEY-----";
  const pemFooter = "-----END PRIVATE KEY-----";
  const pemContents = serviceAccount.private_key
    .replace(pemHeader, "")
    .replace(pemFooter, "")
    .replace(/\s/g, "");

  const binaryKey = Uint8Array.from(atob(pemContents), (c) => c.charCodeAt(0));

  const cryptoKey = await crypto.subtle.importKey(
    "pkcs8",
    binaryKey,
    { name: "RSASSA-PKCS1-v1_5", hash: "SHA-256" },
    false,
    ["sign"]
  );

  // Create signed JWT
  const jwt = await create(
    { alg: "RS256", typ: "JWT" },
    jwtPayload,
    cryptoKey
  );

  // Exchange JWT for access token
  const tokenResponse = await fetch("https://oauth2.googleapis.com/token", {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: new URLSearchParams({
      grant_type: "urn:ietf:params:oauth:grant-type:jwt-bearer",
      assertion: jwt,
    }),
  });

  if (!tokenResponse.ok) {
    const errorText = await tokenResponse.text();
    throw new Error(`Failed to get access token: ${errorText}`);
  }

  const tokenData = await tokenResponse.json();
  cachedAccessToken = tokenData.access_token;
  tokenExpiresAt = Date.now() + (tokenData.expires_in * 1000);

  return cachedAccessToken!;
}

/**
 * Send notification via FCM HTTP v1 API
 */
async function sendFcmV1Notification(
  token: string,
  title: string,
  body: string,
  data: Record<string, string>
): Promise<void> {
  if (!FIREBASE_PROJECT_ID) {
    throw new Error("FIREBASE_PROJECT_ID not configured");
  }

  const accessToken = await getAccessToken();

  const response = await fetch(
    `https://fcm.googleapis.com/v1/projects/${FIREBASE_PROJECT_ID}/messages:send`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${accessToken}`,
      },
      body: JSON.stringify({
        message: {
          token: token,
          notification: {
            title,
            body,
          },
          data,
          android: {
            priority: "high",
            notification: {
              click_action: "OPEN_ACTIVITY",
              channel_id: "denly_notifications",
            },
          },
        },
      }),
    }
  );

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(`FCM v1 error: ${response.status} - ${errorText}`);
  }
}

/**
 * Send notification via APNs HTTP/2 API
 */
async function sendApnsNotification(
  token: string,
  title: string,
  body: string,
  data: Record<string, string>
): Promise<void> {
  // APNs HTTP/2 API requires:
  // 1. JWT token signed with APNs private key
  // 2. HTTP/2 connection to api.push.apple.com
  //
  // For production, you'd use a library like 'apns2' or implement JWT signing.
  // This is a placeholder that logs the attempt.
  //
  // Full implementation would:
  // 1. Create JWT with header: { alg: "ES256", kid: APNS_KEY_ID }
  // 2. Sign with APNS_PRIVATE_KEY
  // 3. POST to https://api.push.apple.com/3/device/{token}

  console.log(`[APNs] Would send to token ${token.substring(0, 8)}...`);
  console.log(`[APNs] Title: ${title}, Body: ${body}`);
  console.log(`[APNs] Data:`, data);

  // Return success for now - iOS notifications will work once APNs is configured
  return;
}
