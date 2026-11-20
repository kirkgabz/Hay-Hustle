-- Hay Hustle backend (Supabase / Postgres)
-- NOT applied yet. Apply when the game is ready to connect to the cloud:
-- accounts, cloud saves and cheat-proof daily rewards (server time).

create table if not exists public.players (
  id          uuid primary key references auth.users(id) on delete cascade,
  farm_name   text not null default 'My Farm',
  character   text not null check (character in ('boy', 'girl')),
  created_at  timestamptz not null default now()
);

-- Whole farm state as JSON (same shape as the game's local SaveData).
create table if not exists public.farm_saves (
  player_id   uuid primary key references auth.users(id) on delete cascade,
  data        jsonb not null,
  updated_at  timestamptz not null default now()
);

create table if not exists public.daily_rewards (
  player_id        uuid primary key references auth.users(id) on delete cascade,
  streak           int  not null default 0,   -- last claimed day (1..7)
  last_claim_date  date
);

alter table public.players       enable row level security;
alter table public.farm_saves    enable row level security;
alter table public.daily_rewards enable row level security;

create policy "own player row"   on public.players    for all using (auth.uid() = id)        with check (auth.uid() = id);
create policy "own farm save"    on public.farm_saves for all using (auth.uid() = player_id) with check (auth.uid() = player_id);
-- Players can read their streak, but only the function below can change it.
create policy "read own rewards" on public.daily_rewards for select using (auth.uid() = player_id);

-- Claim today's reward using SERVER time (changing the phone/PC clock does nothing).
create or replace function public.claim_daily_reward()
returns table (reward_day int, current_streak int)
language plpgsql
security definer
set search_path = public
as $$
declare
  uid      uuid := auth.uid();
  today    date := (now() at time zone 'utc')::date;
  rec      public.daily_rewards;
  next_day int;
begin
  if uid is null then
    raise exception 'not signed in';
  end if;

  insert into public.daily_rewards (player_id) values (uid) on conflict (player_id) do nothing;
  select * into rec from public.daily_rewards where player_id = uid for update;

  if rec.last_claim_date = today then
    raise exception 'already claimed today';
  end if;

  if rec.last_claim_date = today - 1 then
    next_day := (rec.streak % 7) + 1;   -- streak continues
  else
    next_day := 1;                      -- first claim or missed a day
  end if;

  update public.daily_rewards
     set streak = next_day, last_claim_date = today
   where player_id = uid;

  return query select next_day, next_day;
end;
$$;

revoke all on function public.claim_daily_reward() from public, anon;
grant execute on function public.claim_daily_reward() to authenticated;
