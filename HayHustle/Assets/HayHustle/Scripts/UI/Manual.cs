namespace HayHustle
{
    /// <summary>In-game user manual: what to click for every action. Shown by the Manual panel (H).</summary>
    public static class Manual
    {
        public static readonly string[] Titles =
        {
            "Controls", "Water & Grass", "Collecting", "Buildings", "Selling",
            "Animals", "Predators", "Helpers", "Rank & Rewards"
        };

        public static readonly string[] Pages =
        {
            // ---------------- Controls ----------------
            "<b>MOUSE (main way to play)</b>\n" +
            "<b>Left-click anything</b> on the farm and your farmer walks there and does the action.\n" +
            "Move the mouse over something first - a tooltip by the cursor says what the click will do (\"Click: Refill water bucket\").\n" +
            "<b>Left-click the ground</b> - walk there.\n" +
            "<b>Right mouse button + drag</b> - rotate the camera.\n" +
            "<b>Mouse wheel</b> - zoom in / out.\n\n" +
            "<b>BUTTONS (bottom right of the screen)</b>\n" +
            "<b>Shop</b> - buy and sell animals, buy helpers.   <b>Rewards</b> - daily login reward.   <b>Manual</b> - this guide.\n\n" +
            "<b>KEYBOARD (optional)</b>\n" +
            "<b>W A S D</b> or <b>arrow keys</b> - walk     <b>Shift</b> (hold) - run\n" +
            "<b>E</b> - use the closest thing (shown in the bar at the bottom of the screen)\n" +
            "<b>F</b> or <b>Space</b> - swing the net\n" +
            "<b>B</b> - Shop     <b>R</b> - Daily rewards     <b>H</b> - Manual     <b>Esc</b> - close a window\n\n" +
            "<b>WHAT YOU SEE ON SCREEN</b>\n" +
            "Top left: farm name, coins, water in your bucket, Farm Rank and XP, warehouse space, number of animals.\n" +
            "Below it: the goods in your warehouse.\n" +
            "Top right: day and time, day/night, today's WANTED product, predators caught and animals lost.\n" +
            "Right side: news messages. Floating text above things shows what just happened.",

            // ---------------- Water & Grass ----------------
            "<b>1. Get water</b>\n" +
            "<b>Click the WELL</b> (stone well with a little roof, in the yard).\n" +
            "Your farmer cranks the bucket and it fills to full: \"+10 water\".\n\n" +
            "<b>2. Plant grass</b>\n" +
            "<b>Click an empty brown plot</b> inside the fenced pasture.\n" +
            "Your farmer crouches and plants. Each plot uses <b>1 water</b>.\n" +
            "If the bucket is empty, the tooltip says so - go back to the well.\n\n" +
            "<b>3. Animals eat by themselves</b>\n" +
            "Hungry animals walk to the nearest grass and eat it down. Short grass turns yellow-green, then disappears.\n" +
            "Keep planting so there is always grass.\n\n" +
            "<b>Hunger</b>\n" +
            "Well fed: produces at full speed.\n" +
            "Hungry: produces at half speed and sells for less.\n" +
            "Starving for 45 seconds: the animal dies.\n\n" +
            "<b>Tips</b>\n" +
            "Grass planted near chickens grows thicker (Chicken perk).\n" +
            "Cows make the grass under them grow back (Cow perk).",

            // ---------------- Collecting ----------------
            "<b>Collect goods</b>\n" +
            "Animals drop goods on the ground: eggs, wool, milk, goat milk, truffles, feathers, llama wool.\n" +
            "<b>Click the item</b> - your farmer walks over, bends down and picks it up: \"+1 Egg\".\n" +
            "(Walking over an item also collects it.)\n\n" +
            "<b>Spoiling</b>\n" +
            "Items left on the ground spoil. When an item starts <b>blinking</b>, it is about to spoil - grab it!\n" +
            "Spoiled food attracts rats.\n\n" +
            "<b>Warehouse</b>\n" +
            "Everything you collect goes into the warehouse (list at the top left).\n" +
            "If it is full, you cannot pick anything up.\n" +
            "<b>Click the WAREHOUSE building</b> (grey-blue, east side) to upgrade it: +20 space for coins.",

            // ---------------- Buildings ----------------
            "<b>How to use a building</b>\n" +
            "1. Have the needed goods in your warehouse.\n" +
            "2. <b>Click the building</b> - it starts making the product (\"Making Cake... 15s\").\n" +
            "3. When it is done, <b>click the building again</b> to collect (\"+1 Cake\"). It starts the next one automatically if you have enough goods.\n\n" +
            "<b>The ball on the roof shows the status</b>\n" +
            "White = waiting     Yellow = working     Green = ready, click to collect     Grey = locked\n\n" +
            "<b>Buildings</b>\n" +
            "Bakery (Rank 2): 2 Eggs -> Cake (15s)\n" +
            "Weaving Mill (Rank 3): 1 Wool -> Yarn (20s)\n" +
            "Dairy (Rank 5): 1 Milk -> Butter (25s)\n" +
            "Cheese Maker (Rank 7): 2 Goat Milk -> Cheese (30s)\n" +
            "Pillow Factory (Rank 11): 3 Feathers -> Pillow (30s)\n" +
            "Tailor (Rank 14): 2 Llama Wool -> Luxury Coat (45s)\n\n" +
            "Made products are worth much more than raw goods.",

            // ---------------- Selling ----------------
            "<b>Sell goods with the truck</b>\n" +
            "1. <b>Click the TRUCK</b> (blue truck on the road, north of the yard). The truck window opens.\n" +
            "2. For each product: <b>+1</b> loads one, <b>+All</b> loads all of it, <b>Return</b> puts it back in the warehouse.\n" +
            "3. Click <b>Send truck</b>. It drives to town and comes back after 24 seconds with your coins.\n" +
            "The truck holds 20 items per trip.\n\n" +
            "<b>WANTED product</b>\n" +
            "Every farm day one product sells for <b>double price</b>. It is shown at the top right and marked WANTED x2 in the truck.\n\n" +
            "<b>Sell animals</b>\n" +
            "Click <b>Shop</b> (or B) -> <b>Sell animals</b> tab -> <b>Sell 1</b>.\n" +
            "Prime and rare animals sell for more. Babies and hungry animals sell for less.\n\n" +
            "<b>Other ways to earn coins</b>\n" +
            "Catching predators (they are sold to the zoo) and daily rewards.",

            // ---------------- Animals ----------------
            "<b>Buy animals</b>\n" +
            "Click <b>Shop</b> -> <b>Buy animals</b>. A baby is cheap but must grow up first. An adult produces right away.\n\n" +
            "<b>Growth</b>\n" +
            "Baby -> Adult (after its grow time) -> <b>Prime</b> (stay well fed for about 2.5 minutes). Prime animals produce 50% faster.\n\n" +
            "<b>Breeding</b>\n" +
            "Chickens: <b>click the NEST</b> (straw circle next to the well). Uses 1 egg from the warehouse. A chick hatches after 25s. Small chance of a Golden Hen!\n" +
            "Sheep and cows: <b>click the BREEDING BARN</b> (big red barn, Rank 4). Needs 2 well-fed adults of the same kind. A baby is born after 40s. Two Prime parents have a 10% chance of a RARE baby.\n\n" +
            "<b>Animals and perks</b>\n" +
            "Chicken (Rank 1) - Eggs. Grass near chickens grows thicker.\n" +
            "Sheep (Rank 3) - Wool. Hears predators early (+1.5s warning).\n" +
            "Cow (Rank 5) - Milk. Makes grass regrow.\n" +
            "Goat (Rank 7) - Goat milk. Headbutts snakes and rats.\n" +
            "Pig (Rank 9) - Truffles. Cleans spoiled food so fewer rats come.\n" +
            "Turkey (Rank 11) - Feathers. Hawks won't dive near turkeys.\n" +
            "Llama (Rank 14) - Llama wool. Kicks foxes, wolves and bears.\n\n" +
            "<b>Rare breeds</b>: Golden Hen, Cloud Sheep, Brave Cow (kicks predators), Mountain Goat, Truffle King, Royal Turkey, Snow Llama. They produce 2x faster and sell for 5x.",

            // ---------------- Predators ----------------
            "<b>When a predator comes</b>\n" +
            "You hear a warning sound and a message says where it comes from (north / east / south / west).\n" +
            "A <b>red marker</b> at the edge of the screen points to it until it is on screen.\n" +
            "On screen, a <b>red bar above it shows how many hits are left</b>.\n\n" +
            "<b>How to catch it</b>\n" +
            "<b>Click the predator.</b> Your farmer runs to it and swings the net.\n" +
            "<b>Every click = 1 hit.</b> \"HIT! 2 left\" - keep clicking until \"CAUGHT!\".\n" +
            "(Or stand next to it and press F / Space.)\n" +
            "If it grabs an animal it runs back to the forest - chase it! Hits can make it drop the animal.\n\n" +
            "<b>Predators</b>\n" +
            "Fox (Rank 1) - 3 hits. Steals chickens and turkeys.\n" +
            "Hawk (Rank 2) - 2 hits. Flies high, dives for chickens. You can only hit it while it dives low.\n" +
            "Wolf (Rank 3, night only) - 6 hits. Takes sheep, cows and goats.\n" +
            "Snake (Rank 6) - 2 hits. Hides in tall grass (invisible until you are close) and eats eggs.\n" +
            "Rats (Rank 10) - 1 hit each, come in groups. Steal goods from the warehouse. Catch them to get the goods back.\n" +
            "Bear (Rank 15, night only) - 12 hits. Takes big animals.\n\n" +
            "<b>Night is more dangerous</b>: more predators and wolves/bears appear. Your farmer carries a lantern.",

            // ---------------- Helpers ----------------
            "<b>Helpers fight predators for you</b>\n" +
            "Click <b>Shop</b> -> <b>Helpers</b> -> <b>Buy</b>. You can have up to 2 of each.\n\n" +
            "<b>Cat</b> (Rank 6, 300 coins)\n" +
            "Waits by the warehouse. Catches rats and finds snakes hiding in the grass.\n\n" +
            "<b>Guard Dog</b> (Rank 8, 900 coins)\n" +
            "Patrols the pasture. Chases and bites foxes, wolves and bears.\n\n" +
            "<b>Animals that defend themselves</b>\n" +
            "Goats headbutt snakes and rats. Llamas and the rare Brave Cow kick foxes, wolves and bears.\n" +
            "Turkeys keep hawks away. Adult sheep warn you earlier.",

            // ---------------- Rank & Rewards ----------------
            "<b>Farm Rank</b>\n" +
            "Almost everything gives XP: planting, collecting, making products, selling, breeding, catching predators.\n" +
            "Higher rank unlocks new animals, buildings and helpers - and new predators. Max rank is 20.\n\n" +
            "<b>Daily rewards</b>\n" +
            "Click <b>Rewards</b> (or R) -> <b>Claim</b> once per day.\n" +
            "Day 1: 100 coins   Day 2: water + 50 coins   Day 3: free chick   Day 4: 250 coins\n" +
            "Day 5: 6 eggs   Day 6: free adult sheep   Day 7: Mystery chest (maybe a Golden Hen!)\n" +
            "Missing a day resets the streak to Day 1.\n\n" +
            "<b>Saving</b>\n" +
            "The game saves automatically every 30 seconds and when you quit.\n" +
            "On the title screen, <b>Continue</b> loads your farm. <b>New Farm</b> starts over (click twice to confirm).",
        };
    }
}
