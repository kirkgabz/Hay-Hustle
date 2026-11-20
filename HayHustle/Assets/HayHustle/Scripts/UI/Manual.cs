namespace HayHustle
{
    /// <summary>In-game user manual: what to click for every action. Shown by the Manual window (H).</summary>
    public static class Manual
    {
        public static readonly string[] Titles =
        {
            "Controls", "Water & Grass", "Collecting", "Buildings", "Selling",
            "Animals", "Predators", "Defense", "Workers", "Fields", "Orders", "Zombies", "Gates & Wood", "Survivors", "Crops & Food", "Rank & Rewards", "Menu & Settings"
        };

        public static readonly string[] Pages =
        {
            // ---------------- Controls ----------------
            "<b>MOUSE (main way to play)</b>\n" +
            "<b>Left-click anything</b> on the farm and your farmer walks there and does the action.\n" +
            "Move the mouse over something first - a tooltip by the cursor says what the click will do (\"Click: Refill water bucket\").\n" +
            "<b>Left-click the ground</b> - walk there.\n" +
            "<b>Right mouse button + drag</b> - rotate the camera.     <b>Mouse wheel</b> - zoom.\n\n" +
            "<b>BUTTONS (bottom right)</b>\n" +
            "<b>Shop</b> - animals, helpers, workers, fields, fences, traps, sell animals.\n" +
            "<b>Orders</b> - villager orders.   <b>Rewards</b> - daily login reward.   <b>Manual</b> - this guide.   <b>Menu</b> - pause & settings.\n\n" +
            "<b>KEYBOARD (optional)</b>\n" +
            "<b>W A S D</b> / arrows - walk     <b>Shift</b> - run     <b>E</b> - use the closest thing\n" +
            "<b>F</b> or <b>Space</b> - swing the net     <b>B</b> Shop   <b>O</b> Orders   <b>R</b> Rewards   <b>H</b> Manual   <b>Esc</b> Menu / close\n\n" +
            "<b>\"ARE YOU SURE?\" POPUPS</b>\n" +
            "Buying, selling, hiring and other important actions ask you to <b>Confirm</b> or <b>Cancel</b> first.\n" +
            "You can turn this off in Menu > Settings > Confirm buys > Don't ask.\n\n" +
            "<b>ON SCREEN</b>\n" +
            "Top left: farm, coins, water, Farm Rank, warehouse space, animals. Below: your goods.\n" +
            "Top right: day and time, WANTED product, predators caught and animals lost. Right side: news.\n" +
            "A yellow <b>!</b> above an animal means it is hungry (red = starving).",

            // ---------------- Water & Grass ----------------
            "<b>1. Get water</b>\n" +
            "<b>Click the WELL</b> (stone well in the yard). Your farmer cranks the bucket and it fills up: \"+10 water\".\n\n" +
            "<b>2. Plant grass</b>\n" +
            "<b>Click an empty brown plot</b> inside a fenced field. Each plot uses <b>1 water</b>.\n" +
            "If the bucket is empty, the tooltip says so - go back to the well.\n\n" +
            "<b>3. Animals eat by themselves</b>\n" +
            "Hungry animals walk to the nearest grass in their field and eat it down. Keep planting!\n\n" +
            "<b>Hunger</b>\n" +
            "Yellow ! above an animal = hungry: it produces at half speed and sells for less.\n" +
            "Red ! = starving: after 45 seconds it dies.\n\n" +
            "<b>Tips</b>\n" +
            "Grass planted near chickens grows thicker. Cows make the grass under them grow back.\n" +
            "Hire a <b>Water Carrier</b> (Shop > Workers) to replant plots for you.",

            // ---------------- Collecting ----------------
            "<b>Collect goods</b>\n" +
            "Animals drop goods on the ground - eggs, wool, milk, goat milk, truffles, feathers, llama wool. " +
            "Each item has a <b>glowing yellow ring</b> under it so you can spot it.\n" +
            "<b>Click the item</b> - your farmer walks over and picks it up: \"+1 Egg\". (Walking over it also works.)\n\n" +
            "<b>Spoiling</b>\n" +
            "When an item starts <b>blinking</b>, it is about to spoil - grab it! Spoiled food attracts rats.\n\n" +
            "<b>Warehouse</b>\n" +
            "Everything goes into the warehouse. When it is full you can't pick anything up.\n" +
            "<b>Click the WAREHOUSE</b> (grey-blue building, east side) to open it: upgrade it (+20 space) or Quick Sell goods.\n\n" +
            "Hire a <b>Collector</b> (Shop > Workers) to pick up goods for you.",

            // ---------------- Buildings ----------------
            "<b>How to use a building</b>\n" +
            "1. Have the needed goods in your warehouse.\n" +
            "2. <b>Click the building</b> - it starts making the product (\"Making Cake... 15s\").\n" +
            "3. When it is done, <b>click it again</b> to collect (\"+1 Cake\"). It starts the next one automatically if you have enough goods.\n\n" +
            "<b>The ball on the roof</b>:  White = waiting   Yellow = working   Green = ready   Grey = locked\n\n" +
            "<b>Buildings</b>\n" +
            "Bakery (Rank 2): 2 Eggs -> Cake (15s)\n" +
            "Weaving Mill (Rank 3): 1 Wool -> Yarn (20s)\n" +
            "Dairy (Rank 5): 1 Milk -> Butter (25s)\n" +
            "Cheese Maker (Rank 7): 2 Goat Milk -> Cheese (30s)\n" +
            "Pillow Factory (Rank 11): 3 Feathers -> Pillow (30s)\n" +
            "Tailor (Rank 14): 2 Llama Wool -> Luxury Coat (45s)\n\n" +
            "Made products are worth much more. A <b>Factory Hand</b> (Shop > Workers) runs the buildings for you.",

            // ---------------- Selling ----------------
            "<b>1. Truck (best price)</b>\n" +
            "<b>Click the blue TRUCK</b> (on the road north of the yard). For each product: <b>+1</b>, <b>+All</b> or <b>Return</b>.\n" +
            "Click <b>Send truck</b> and confirm. It comes back after 24 seconds with your coins. 20 items per trip.\n\n" +
            "<b>2. Quick Sell (instant, 70% price)</b>\n" +
            "<b>Click the WAREHOUSE</b> -> <b>Sell 1</b> or <b>Sell all</b>. You get the coins right away.\n\n" +
            "<b>3. Orders (bonus price)</b>\n" +
            "Deliver villager orders at the Order Board for about 1.6x the normal price.\n\n" +
            "<b>WANTED product</b>: every farm day one product sells for <b>double</b> (top right).\n\n" +
            "<b>Sell animals</b>: Shop -> <b>Sell animals</b> -> <b>Sell 1</b>. Prime and rare animals are worth more.\n\n" +
            "A <b>Driver</b> (Shop > Workers) loads and sends the truck for you, and keeps enough raw goods for your buildings.",

            // ---------------- Animals ----------------
            "<b>Buy animals</b>\n" +
            "Shop -> <b>Animals</b>. A baby is cheap but must grow first. An adult produces right away. " +
            "Each kind lives in its own field - buy the field first (see Fields).\n\n" +
            "<b>Growth</b>: Baby -> Adult -> <b>Prime</b> (well fed for about 2.5 minutes, +50% production).\n\n" +
            "<b>Breeding</b>\n" +
            "Chickens: <b>click the NEST</b> (straw circle next to the well). Uses 1 egg. A chick hatches after 25s (small chance of a Golden Hen!).\n" +
            "Sheep and cows: <b>click the BREEDING BARN</b> (red barn, Rank 4) with 2 well-fed adults of the same kind. Baby after 40s. Two Prime parents: 10% rare baby.\n\n" +
            "<b>Animals and perks</b>\n" +
            "Up to 5 of each kind (one field each).\n" +
            "Chicken (Rank 1) - Eggs. Grass near chickens grows thicker.\n" +
            "Sheep (Rank 3) - Wool. Hears predators early (+1.5s warning).\n" +
            "Cow (Rank 5) - Milk. Makes grass regrow.\n" +
            "Goat (Rank 7) - Goat milk. Headbutts snakes and rats.\n" +
            "Pig (Rank 9) - Truffles. Cleans spoiled food so fewer rats come.\n" +
            "Turkey (Rank 11) - Feathers. Hawks won't dive near turkeys.\n" +
            "Llama (Rank 14) - Llama wool. Kicks foxes, wolves and bears.\n\n" +
            "<b>Rare breeds</b>: Golden Hen, Cloud Sheep, Brave Cow, Mountain Goat, Truffle King, Royal Turkey, Snow Llama - 2x faster, sell for 5x.",

            // ---------------- Predators ----------------
            "<b>When a predator comes</b>\n" +
            "A warning sound plays and a message says the direction. A <b>red marker</b> at the screen edge points to it. " +
            "On screen, a <b>red bar above it shows how many hits are left</b>.\n\n" +
            "<b>How to catch it</b>\n" +
            "<b>Click the predator.</b> Your farmer runs to it and swings the net. <b>Every click = 1 hit</b> - \"HIT! 2 left\" ... \"CAUGHT!\".\n" +
            "If it grabs an animal, it runs to the forest - chase it! Hits can make it drop the animal.\n\n" +
            "<b>Predators</b>\n" +
            "Fox (Rank 1) - 3 hits. Steals chickens and turkeys.\n" +
            "Hawk (Rank 2) - 2 hits. Hit it only while it dives low.\n" +
            "Wolf (Rank 3, night) - 6 hits. Takes sheep, cows and goats.\n" +
            "Snake (Rank 6) - 2 hits. Hides in tall grass, eats eggs.\n" +
            "Rats (Rank 10) - 1 hit each, in groups. Steal from the warehouse - catch them to get the goods back.\n" +
            "Bear (Rank 15, night) - 12 hits. Takes big animals.\n\n" +
            "<b>Difficulty</b>: until you hire a <b>Guard</b> (Shop -> Workers, Rank 3), <b>only foxes</b> come, one at a time. " +
            "With a Guard on duty other predators can come too, but they are weak: about half the hits to catch, and slower.\n\n" +
            "A tall <b>wall</b> goes around the whole farm. Foxes, wolves and bears must <b>break the farm gate</b> to get in (see Gates & Wood).\n\n" +
            "No predators come during the tutorial. Night brings more of them - your farmer carries a lantern.",

            // ---------------- Defense ----------------
            "<b>Helpers</b> (Shop -> Helpers, up to 2 each)\n" +
            "<b>Cat</b> (Rank 6, 300): guards the warehouse, catches rats and finds hidden snakes.\n" +
            "<b>Guard Dog</b> (Rank 8, 900): patrols the fields, bites foxes, wolves and bears.\n\n" +
            "<b>Fences</b> (Shop -> Farm)\n" +
            "Wooden Fence -> <b>Sturdy Fence</b> (1200, predators 30% slower inside your fields) -> <b>Iron Fence</b> (3000, 50% slower).\n\n" +
            "<b>Traps</b> (Shop -> Farm, 150 each, up to 4)\n" +
            "Set automatically at the edge of your fields or by the warehouse. A fox, wolf, snake or rat that steps on one is caught at once. A bear takes 4 hits. Each trap works once.\n\n" +
            "<b>Animals that defend</b>\n" +
            "Goats headbutt snakes and rats. Llamas and the rare Brave Cow kick foxes, wolves and bears. Turkeys keep hawks away. Adult sheep warn you earlier.\n\n" +
            "<b>Guards</b> (Shop -> Workers): hired guards chase and catch predators for you.\n\n" +
            "<b>Lamp posts</b> (Shop -> <b>Lights</b>, 250 each, up to 10)\n" +
            "<b>You choose where it goes</b>: after buying, a see-through lamp follows your mouse with a ring showing how far the light reaches - " +
            "green = OK, red = not allowed. Click to build it there (you pay when it is placed). <b>Esc</b> cancels.\n" +
            "Lamps go anywhere inside the farm wall (not in the gateway, not right next to another lamp). <b>Click a lamp post</b> later to move it for free.\n" +
            "They turn <b>on at 18:00</b> and <b>off at 06:00</b> by themselves. Predators move 40% slower near a lit lamp and leave animals in the light alone when they can.",

            // ---------------- Workers ----------------
            "<b>Hire workers</b>: Shop -> <b>Workers</b> -> <b>Hire</b> (confirm).\n" +
            "You pay a hiring fee now and a <b>wage every morning</b>. If you can't pay the wage, the worker quits.\n" +
            "Workers walk around the farm; their name and current job float above their head.\n\n" +
            "<b>Collector</b> (Rank 2, hire 400, 40/day, up to 4)\n" +
            "Picks up goods before they spoil.\n\n" +
            "<b>Water Carrier</b> (Rank 3, hire 500, 50/day, up to 4)\n" +
            "Fetches water from the well and replants empty plots in all your fields.\n\n" +
            "<b>Factory Hand</b> (Rank 5, hire 800, 80/day)\n" +
            "Starts products in your buildings and collects them when ready.\n\n" +
            "<b>Driver</b> (Rank 6, hire 600, 60/day)\n" +
            "When there are enough goods, loads the truck (WANTED first, then the most valuable) and sends it. " +
            "Keeps 6 of every raw good your buildings and nest need.\n\n" +
            "<b>Guard</b> (Rank 3, hire 1200, 100/day, up to 4)\n" +
            "Patrols your fields with a pitchfork. Runs after any predator and fights it until it is caught (a hawk only when it dives low).\n\n" +
            "<b>Which field do they work in?</b>\n" +
            "Collectors, Water Carriers and Guards work in <b>All fields (rotation)</b> by default - they visit your fields one after another. " +
            "In Shop -> Workers, click a worker's <b>Works in</b> button to give them one field instead (click again to go to the next field). " +
            "Hire up to 4 of each, e.g. one Collector for the Chicken Run and one for the Sheep Meadow.\n\n" +
            "<b>Dismiss one</b> sends a worker away (no refund).",

            // ---------------- Fields ----------------
            "<b>Every kind of animal has its own field</b>\n" +
            "Every field is the same size: <b>4 x 4 grass plots</b> and room for <b>5 animals</b>. They sit in a neat grid behind the yard, so they are all close by.\n" +
            "Your chickens live in the <b>Chicken Run</b> (yours from the start). To keep any other animal, first buy its field:\n" +
            "click its <b>FOR SALE sign</b>, or Shop -> <b>Farm</b> -> <b>Buy</b>, or the <b>Buy field</b> button next to the animal in Shop -> Animals.\n\n" +
            "Chicken Run - chickens, free (front left)\n" +
            "Sheep Meadow - Rank 3, 1000 coins (front right)\n" +
            "Cow Pasture - Rank 5, 2500 coins (middle left)\n" +
            "Goat Hill - Rank 7, 3500 coins (middle right)\n" +
            "Pig Pen - Rank 9, 5000 coins (back left)\n" +
            "Turkey Yard - Rank 11, 6500 coins (back right)\n" +
            "Llama Ridge - Rank 14, 9000 coins (at the back)\n\n" +
            "A sign over each field's entrance shows how many animals are inside, e.g. \"Chicken Run 3/5\" (red = FULL).\n" +
            "Keep the plots planted so your 5 animals never go hungry - or hire a Water Carrier.",

            // ---------------- Orders ----------------
            "<b>Order Board</b>\n" +
            "<b>Click the ORDER BOARD</b> (notice board in the yard, next to the truck) or the <b>Orders</b> button.\n" +
            "Up to 3 villagers ask for goods, e.g. \"5 Eggs + 2 Cakes\". Each order shows what you have (green = enough, red = missing) and the time left.\n\n" +
            "Click <b>Deliver</b> (confirm) to hand over the goods for the reward - about <b>1.6x</b> the normal price, plus XP.\n" +
            "Missing goods are red, and below each one the order tells you <b>how to get it</b> (e.g. \"Click the Bakery to make it from 2 Eggs\").\n" +
            "Click <b>Show</b> and a green <b>GO HERE</b> arrow guides you to the right building or field. Click <b>Stop</b> to hide it.\n" +
            "Orders only ask for goods you are able to make.\n" +
            "Click <b>Discard</b> to remove an order you can't do. New orders arrive every ~25 seconds.\n" +
            "If the time runs out, the order disappears.",

            // ---------------- Zombies ----------------
            "<b>The farm is surrounded by the undead</b>\n" +
            "From <b>Day 2</b>, zombies shamble out of the forest - in the day and (more of them) at night. You hear a groan when they come.\n" +
            "They are slow but tough: about <b>5 hits</b> (3 when you have defenders). Click a zombie to hit it, like any predator.\n\n" +
            "<b>What zombies do</b>\n" +
            "They walk around the farm wall to the <b>farm gate</b> (where the road comes in) and smash it. A bar over the gate shows its strength. " +
            "When the gate breaks they walk into the farm and drag an animal away - chase them!\n" +
            "They shove the farmer back (you are never hurt) and bite survivors (survivors get injured, never killed).\n\n" +
            "<b>How to fight back</b>\n" +
            "<b>Repair the farm gate</b> with wood (see Gates & Wood), or give survivors the <b>Builder</b> job.\n" +
            "<b>Guards</b> (hired workers or survivors on guard duty), <b>Guard Dogs</b>, <b>traps</b>, <b>lamp posts</b> (zombies are slower in the light), " +
            "and <b>Llamas</b> / the Brave Cow (they kick zombies).\n" +
            "Until you have a defender only one zombie comes at a time; after that, bigger hordes come at night.",

            // ---------------- Gates & Wood ----------------
            "<b>The farm wall and gate</b>\n" +
            "A tall wooden wall goes around the <b>whole farm</b>. It has <b>one gate</b>, on the road at the north side, with <b>10,000 health</b>. " +
            "Fields and the Crop Field have open entrances - the farm gate is what keeps danger out.\n" +
            "Zombies, foxes, wolves and bears walk around the wall and hit the gate again and again (zombies never stop; animals give up after a while). " +
            "Once it is broken they walk right in. Hawks fly over the wall, snakes and rats slip under it.\n" +
            "The gate opens by itself for the truck, you and your people, and stays shut while danger is near.\n" +
            "A bar over the gate shows its health when it is damaged. Damage stays until it is repaired.\n\n" +
            "<b>Repairing</b>\n" +
            "Repairing adds <b>100 health per minute for each person</b> working on it - two builders = 200 per minute.\n" +
            "It uses <b>wood</b>: 1 wood for every 100 health.\n" +
            "<b>You</b>: click the damaged gate - your farmer keeps hammering (100 per minute) until you walk away or run out of wood.\n" +
            "<b>Builders</b> (survivor job): they go to the farm gate by themselves and stand side by side to repair it.\n\n" +
            "<b>Wood</b>\n" +
            "Two groves of trees grow beside the fields. <b>Click a tree</b> to chop it (5 seconds, +3 wood). " +
            "Builders chop trees by themselves with an axe when the wood runs out. A stump grows back after 3 minutes.\n" +
            "Your wood is shown at the top left (a new farm starts with 10).\n\n" +
            "<b>Guards</b>\n" +
            "Guards defend the whole farm, from the middle out to <b>2 plots beyond the wall and gate</b> - they patrol between their field and the farm gate and do not chase predators further away.\n" +
            "<b>Weapons</b> (Shop -> Workers, or the Camp): Pitchfork -> Machete -> Spear -> Battle Axe -> Flaming Sword. " +
            "Every guard - hired or survivor - uses the best one you bought. Zoom in (mouse wheel) to see them.\n\n" +
            "<b>Guard health</b>\n" +
            "Zombies, bears, wolves and foxes hurt guards who fight them (a bar under their name shows their health). Guards are never killed.\n" +
            "Guards <b>heal by themselves</b>, but it <b>costs food</b>: a hurt guard eats one food item from storage (wheat, eggs, milk first, meals last) - " +
            "1 food point heals 20 health, about 2 health per second.\n" +
            "Below 30% health a guard stops fighting until healed. <b>No food = no healing</b>, so keep food in storage.",

            // ---------------- Survivors ----------------
            "<b>Survivors join your farm</b>\n" +
            "Now and then a survivor walks up the road. A card shows their name and skill - click <b>Accept</b> or <b>Decline</b>. " +
            "They need a <b>free bed</b> in the Bunkhouse.\n\n" +
            "<b>Camp window</b> (Camp button, C, or click the <b>Bunkhouse</b> next to the road)\n" +
            "See every survivor's health, morale and what they are doing. Click <b>Job</b> to open the list of jobs and pick one. " +
            "Use the <b>Show</b> buttons to filter by job - each shows how many survivors do it, listed by name (A-Z).\n" +
            "<b>Guard</b> - fights zombies and predators on the farm.   <b>Builder</b> - chops trees and repairs the farm gate.\n" +
            "<b>Farmer</b> - plants, waters and harvests crops.   <b>Cook</b> - makes meals in the Kitchen.\n" +
            "<b>Collector</b> - picks up goods.   <b>Water Carrier</b> - replants grass.   <b>Off duty</b> - rests.\n" +
            "Survivors work <b>40% faster at their skill</b>. Collectors, Guards and Water Carriers can also be given one field (\"Works in\").\n\n" +
            "<b>Food and morale</b>\n" +
            "Survivors cost no coins, but every morning each one eats <b>3 food</b>. Fed survivors get happier. " +
            "Hungry ones lose morale - at 0 they leave the farm.\n\n" +
            "<b>Injuries</b>: a survivor bitten too often is <b>injured</b> and walks to the Bunkhouse to rest until healed.\n" +
            "<b>More beds</b>: Camp -> More beds (+2 per upgrade, up to 10).",

            // ---------------- Crops & Food ----------------
            "<b>Crop Field</b> (fenced field north-west of the yard, next to the Kitchen)\n" +
            "1. Buy seeds: Shop -> <b>Seeds</b> (packs of 1, 5 or 10). A new farm starts with 8 wheat seeds.\n" +
            "2. <b>Click the Crop Field</b> (any empty plot). Pick a seed and press <b>Plant all</b> - every empty plot is planted at once.\n" +
            "3. Press <b>Water all</b> (uses water from your bucket, 1 per plot). Crops only grow when watered.\n" +
            "4. When a crop is ripe, <b>click it to harvest</b>.\n" +
            "Or give a survivor the <b>Farmer</b> job - they do all of it (choose what they plant in the seed window: \"Farmer survivors plant\").\n\n" +
            "<b>Crops</b>\n" +
            "Seeds per crop (Shop -> Seeds):\n" +
            "Wheat (Rank 1) - 5 coins, 60s, x3, 1 food each\n" +
            "Carrot (Rank 2) - 10 coins, 90s, x3, 2 food each\n" +
            "Corn (Rank 3) - 15 coins, 120s, x4, 2 food each\n" +
            "Pumpkin (Rank 5) - 30 coins, 180s, x2, 4 food each\n\n" +
            "<b>Kitchen</b>: 2 Wheat -> 1 Meal (5 food). Click it, or give a survivor the <b>Cook</b> job.\n" +
            "Eggs and milk also count as food (1 each). Your food total is shown at the top left.\n\n" +
            "<b>Barn storage</b>: Shop -> Farm -> Barn storage adds +30 storage per level (up to 5 levels). The barn is reinforced - zombies can't get in.",

            // ---------------- Rank & Rewards ----------------
            "<b>Farm Rank</b>\n" +
            "Almost everything gives XP: planting, collecting, making products, selling, orders, breeding, catching predators.\n" +
            "Higher rank unlocks animals, buildings, helpers, workers and fields - and new predators. Max rank 20.\n\n" +
            "<b>Daily rewards</b>\n" +
            "Click <b>Rewards</b> -> <b>Claim</b> once per day. The button shows <b>Reward!</b> when one is waiting.\n" +
            "Day 1: 100 coins   Day 2: water + 50   Day 3: free chick   Day 4: 250 coins\n" +
            "Day 5: 6 eggs   Day 6: free adult sheep   Day 7: Mystery chest (maybe a Golden Hen!)\n" +
            "Missing a day resets the streak to Day 1.",

            // ---------------- Menu & Settings ----------------
            "<b>Menu</b> (button or Esc) pauses the game.\n\n" +
            "<b>Resume</b> - back to the farm.\n" +
            "<b>Volume</b> - drag the slider.\n" +
            "<b>Graphics</b> - <b>Low</b> turns off shadows and runs faster on laptops. <b>High</b> looks best.\n" +
            "<b>Confirm buys</b> - <b>Ask first</b> shows \"Are you sure?\" popups; <b>Don't ask</b> skips them for buying and selling.\n" +
            "<b>Replay tutorial</b> - shows the step-by-step guide again.\n" +
            "<b>Save & quit to title</b> - saves and goes back to the title screen.\n" +
            "<b>Quit game</b> - saves and closes the game.\n\n" +
            "<b>Account</b>: the first screen is <b>Sign in / Sign up</b> (email + password, no code or email link needed). " +
            "Tick <b>Stay signed in</b> so you don't have to type it every time. <b>Play offline</b> skips signing in. " +
            "Sign out from the title screen (bottom right).\n" +
            "<b>Saving</b>: the game also saves automatically every 30 seconds.\n" +
            "<b>Backups</b>: a copy of your farm is kept every time you click Continue and before a New Farm (the last 3). " +
            "On the title screen, click <b>Restore backup</b> to go back to one of them. Your current save is kept aside first.\n" +
            "On the title screen, <b>Continue</b> loads your farm. <b>New Farm</b> starts over (it asks to confirm).",
        };
    }
}
