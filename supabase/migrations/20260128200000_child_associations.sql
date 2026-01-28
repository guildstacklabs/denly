-- Junction table: events ↔ children (many-to-many)
create table if not exists event_children (
    id uuid primary key default gen_random_uuid(),
    event_id uuid not null references events(id) on delete cascade,
    child_id uuid not null references children(id) on delete cascade,
    den_id uuid not null references dens(id) on delete cascade,
    created_at timestamptz not null default now(),
    unique(event_id, child_id)
);

create index idx_event_children_event on event_children(event_id);
create index idx_event_children_child on event_children(child_id);

-- Junction table: expenses ↔ children (many-to-many)
create table if not exists expense_children (
    id uuid primary key default gen_random_uuid(),
    expense_id uuid not null references expenses(id) on delete cascade,
    child_id uuid not null references children(id) on delete cascade,
    den_id uuid not null references dens(id) on delete cascade,
    created_at timestamptz not null default now(),
    unique(expense_id, child_id)
);

create index idx_expense_children_expense on expense_children(expense_id);
create index idx_expense_children_child on expense_children(child_id);

-- Migrate existing child_id data into junction tables
insert into event_children (event_id, child_id, den_id)
select id, child_id, den_id from events
where child_id is not null
on conflict do nothing;

insert into expense_children (expense_id, child_id, den_id)
select id, child_id, den_id from expenses
where child_id is not null
on conflict do nothing;

-- RLS
alter table event_children enable row level security;
alter table expense_children enable row level security;

create policy "Den members can read event_children"
    on event_children for select
    using (den_id in (select den_id from den_members where user_id = auth.uid()));

create policy "Den members can insert event_children"
    on event_children for insert
    with check (den_id in (select den_id from den_members where user_id = auth.uid()));

create policy "Den members can delete event_children"
    on event_children for delete
    using (den_id in (select den_id from den_members where user_id = auth.uid()));

create policy "Den members can read expense_children"
    on expense_children for select
    using (den_id in (select den_id from den_members where user_id = auth.uid()));

create policy "Den members can insert expense_children"
    on expense_children for insert
    with check (den_id in (select den_id from den_members where user_id = auth.uid()));

create policy "Den members can delete expense_children"
    on expense_children for delete
    using (den_id in (select den_id from den_members where user_id = auth.uid()));
