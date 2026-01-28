// Supabase Edge Function: ICS Calendar Feed
// Serves a live .ics feed for subscribing from Google Calendar / Apple Calendar.
// URL: /functions/v1/calendar-ics?token=<subscription_token>

import { serve } from "https://deno.land/std@0.168.0/http/server.ts";
import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const SUPABASE_URL = Deno.env.get("SUPABASE_URL")!;
const SUPABASE_SERVICE_ROLE_KEY = Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!;

interface CalendarEvent {
  id: string;
  den_id: string;
  title: string;
  event_type: string;
  starts_at: string;
  ends_at: string | null;
  all_day: boolean;
  location: string | null;
  notes: string | null;
  created_at: string;
  updated_at: string;
}

function escapeIcsText(text: string): string {
  return text
    .replace(/\\/g, "\\\\")
    .replace(/;/g, "\\;")
    .replace(/,/g, "\\,")
    .replace(/\n/g, "\\n");
}

function formatIcsDate(dateStr: string, allDay: boolean): string {
  const d = new Date(dateStr);
  if (allDay) {
    // VALUE=DATE format: YYYYMMDD
    const y = d.getUTCFullYear();
    const m = String(d.getUTCMonth() + 1).padStart(2, "0");
    const day = String(d.getUTCDate()).padStart(2, "0");
    return `${y}${m}${day}`;
  }
  // UTC datetime: YYYYMMDDTHHmmSSZ
  return d.toISOString().replace(/[-:]/g, "").replace(/\.\d{3}/, "");
}

function buildVEvent(event: CalendarEvent): string {
  const lines: string[] = [];
  lines.push("BEGIN:VEVENT");
  lines.push(`UID:${event.id}@denly.app`);

  if (event.all_day) {
    lines.push(`DTSTART;VALUE=DATE:${formatIcsDate(event.starts_at, true)}`);
    if (event.ends_at) {
      // ICS all-day end date is exclusive, so add 1 day
      const end = new Date(event.ends_at);
      end.setUTCDate(end.getUTCDate() + 1);
      lines.push(`DTEND;VALUE=DATE:${formatIcsDate(end.toISOString(), true)}`);
    }
  } else {
    lines.push(`DTSTART:${formatIcsDate(event.starts_at, false)}`);
    if (event.ends_at) {
      lines.push(`DTEND:${formatIcsDate(event.ends_at, false)}`);
    }
  }

  lines.push(`SUMMARY:${escapeIcsText(event.title)}`);

  if (event.location) {
    lines.push(`LOCATION:${escapeIcsText(event.location)}`);
  }
  if (event.notes) {
    lines.push(`DESCRIPTION:${escapeIcsText(event.notes)}`);
  }

  lines.push(`CATEGORIES:${event.event_type || "other"}`);
  lines.push(`DTSTAMP:${formatIcsDate(event.updated_at || event.created_at, false)}`);
  lines.push("END:VEVENT");
  return lines.join("\r\n");
}

serve(async (req) => {
  if (req.method !== "GET") {
    return new Response("Method not allowed", { status: 405 });
  }

  const url = new URL(req.url);
  const token = url.searchParams.get("token");

  if (!token) {
    return new Response("Missing token parameter", { status: 400 });
  }

  const supabase = createClient(SUPABASE_URL, SUPABASE_SERVICE_ROLE_KEY);

  // Look up subscription by token
  const { data: sub, error: subError } = await supabase
    .from("calendar_subscriptions")
    .select("den_id, user_id")
    .eq("token", token)
    .single();

  if (subError || !sub) {
    return new Response("Invalid or expired token", { status: 403 });
  }

  // Fetch all events for this den (past 1 year + future 1 year)
  const now = new Date();
  const past = new Date(now);
  past.setFullYear(past.getFullYear() - 1);
  const future = new Date(now);
  future.setFullYear(future.getFullYear() + 1);

  const { data: events, error: eventsError } = await supabase
    .from("events")
    .select("*")
    .eq("den_id", sub.den_id)
    .gte("starts_at", past.toISOString())
    .lte("starts_at", future.toISOString())
    .order("starts_at", { ascending: true });

  if (eventsError) {
    console.error("Error fetching events:", eventsError);
    return new Response("Failed to fetch events", { status: 500 });
  }

  // Build ICS
  const vevents = (events || []).map((e: CalendarEvent) => buildVEvent(e)).join("\r\n");

  const ics = [
    "BEGIN:VCALENDAR",
    "VERSION:2.0",
    "PRODID:-//Denly//Calendar//EN",
    "CALSCALE:GREGORIAN",
    "METHOD:PUBLISH",
    "X-WR-CALNAME:Denly Family Calendar",
    "X-WR-TIMEZONE:UTC",
    vevents,
    "END:VCALENDAR",
  ].join("\r\n");

  return new Response(ics, {
    status: 200,
    headers: {
      "Content-Type": "text/calendar; charset=utf-8",
      "Content-Disposition": 'inline; filename="denly-calendar.ics"',
      "Cache-Control": "no-cache, no-store, must-revalidate",
    },
  });
});
