-- Calendar subscription tokens for ICS feed access
create table if not exists calendar_subscriptions (
    id uuid primary key default gen_random_uuid(),
    den_id uuid not null references dens(id) on delete cascade,
    user_id uuid not null references auth.users(id) on delete cascade,
    token text not null unique,
    created_at timestamptz not null default now(),
    unique(den_id, user_id)
);

create index idx_calendar_subscriptions_token on calendar_subscriptions(token);

-- RLS
alter table calendar_subscriptions enable row level security;

create policy "Users can read own subscriptions"
    on calendar_subscriptions for select
    using (auth.uid() = user_id);

create policy "Users can create own subscriptions"
    on calendar_subscriptions for insert
    with check (auth.uid() = user_id);

create policy "Users can delete own subscriptions"
    on calendar_subscriptions for delete
    using (auth.uid() = user_id);
