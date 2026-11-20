# Hay Hustle

A free-play 3D farm game: grow grass, raise animals, make goods, sell them by truck,
and protect your farm from foxes, hawks and wolves that keep coming from the forest.

Built with **Unity 6 (URP)** and **C#**. PC first, Android later. Backend: **Supabase** (planned).

## Folder layout

| Folder | What's inside |
|---|---|
| `HayHustle/` | The Unity project (open this folder in Unity Hub) |
| `HayHustle/Assets/HayHustle/Scripts` | All game code (C#) |
| `HayHustle/Assets/HayHustle/Resources/Models` | Drop realistic model prefabs here to replace placeholder shapes |
| `backend/supabase/migrations` | Database for accounts, cloud saves, daily rewards (not applied yet) |
| `concept-art` | Concept images of the target look |
| `USER_MANUAL.md` | What to click for every action |

## First-time setup (about 30-60 minutes, mostly downloading)

1. **Install Unity Hub**: https://unity.com/download
2. In Unity Hub > **Installs** > **Install Editor** > choose the latest **Unity 6 LTS** (6000.x).
   - Optional now, needed later: tick **Android Build Support**.
3. In Unity Hub > **Projects** > **New project** > template **Universal 3D** (URP).
   - Project name: `HayHustle`
   - Location: `C:\Hay Hustle`
   - This creates `C:\Hay Hustle\HayHustle`.
4. Or, if you cloned this repo: in Unity Hub click **Add > Add project from disk** and pick the `HayHustle` folder.
5. Back in Unity, wait for it to finish compiling (spinner bottom-right).
6. Open `Assets/Scenes/SampleScene` and press **Play**. The farm builds itself automatically.

### If something goes wrong

- **Red errors about `UnityEngine.InputSystem`**: Window > Package Manager > Unity Registry > install **Input System**,
  then allow the editor restart.
- **Nothing happens when pressing keys**: Edit > Project Settings > Player > Other Settings >
  **Active Input Handling** = `Input System Package (New)` or `Both`.
- **Laggy on this laptop (Intel UHD graphics)**: Edit > Project Settings > Quality > pick the **Mobile** (or lowest) level,
  and in the Game view set the resolution to 1600x900.

## Controls

| Key | Action |
|---|---|
| WASD / arrows | Move |
| Shift | Run |
| E | Use (plant grass, well, nest, buildings, truck, barn, warehouse) |
| Left click / F / Space | Swing the net at predators |
| B | Animal shop (buy / sell) |
| R | Daily rewards |
| H | Help |
| Right mouse drag / scroll | Rotate / zoom camera |
| Esc | Close panel |

## What's in this first playable version

- Character select (boy / girl, fixed designs) and farm name
- Free play, no levels; **Farm Rank** (1-20) unlocks animals, buildings and predators
- Grass planting with a water bucket and well; animals walk to grass and eat
- Chickens (rank 1), sheep (rank 3), cows (rank 5); Baby -> Adult -> Prime; hunger and starving
- Perks: chicken Bug Eater, sheep Flock Alarm, cow Fertilizer, rare Brave Cow kicks
- Products spoil if left on the ground; warehouse with upgrades
- Bakery (eggs -> cake), Weaving Mill (wool -> yarn), Dairy (milk -> butter)
- Nest (egg -> chick) and Breeding Barn (2 adults -> baby, rare breeds possible)
- Buy and sell animals; truck deliveries; daily "Wanted" product at double price
- Foxes, hawks (dive from the sky), wolves (night only); wave warnings at the screen edge;
  predators grab animals and run, so chase and hit them to save the animal
- Day/night cycle (12 min per day), lantern at night, more predators at night
- 7-day daily login rewards with a mystery chest
- Auto-save every 30 seconds and on quit

### Batch 2

- Goat (rank 7, goat milk -> cheese, headbutts snakes/rats), Pig (rank 9, truffles, cleans spoiled food),
  Turkey (rank 11, feathers -> pillow, scares hawks), Llama (rank 14, llama wool -> luxury coat, kicks big predators)
- Snake (rank 6): hides in tall grass, swallows eggs, then goes for chicks
- Rat swarms (rank 10): steal goods from the warehouse; spoiled food left on the ground attracts more
- Bear (rank 15): night boss, 12 hits, grabs cows/pigs/goats/sheep
- Helpers: Cat (rank 6, hunts rats and snakes near the warehouse), Guard Dog (rank 8, hunts foxes/wolves/bears)
- New buildings: Cheese Maker, Pillow Factory, Tailor

### Batch 3

- Sharp, bigger UI text (drawn at real screen resolution), new button style, bottom-right menu buttons
- 3D character select: the real boy/girl farmers on a stage, the chosen one waves
- "Are you sure?" confirm popups for buying, selling, hiring, upgrading, quitting (can be turned off)
- Workers: Collector, Water Carrier, Factory Hand, Driver (hire fee + daily wage)
- Buy more fields (South Field, West Meadow, East Meadow) - more grass and animal space
- Quick Sell from the warehouse (instant, 70% price)
- Order Board with villager orders for bonus coins
- Fence upgrades (predators slower) and traps
- Hunger icons above hungry animals; bigger goods with a glowing ring
- Guided tutorial with Skip; pause menu with volume, Low/High graphics, replay tutorial, save & quit

## Not built yet (next steps)

- Real realistic models and animations (drop prefabs in `Resources/Models`; also `Cat`, `Dog`, `Snake`, `Rat`, `Bear`, `Goat`, `Pig`, `Turkey`, `Llama`)
- Sound and music
- Fences, traps, scarecrow upgrades
- Weather, hay mill, feed mill, order board
- Supabase login, cloud save and server-time daily rewards
- Android touch controls and build
