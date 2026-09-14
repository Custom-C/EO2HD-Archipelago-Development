namespace EO2HD_Patcher.Etrian_Odyssey_2_Data;

public class EnemyDictionaries
{
    //The main purpose of this page is to categorize enemies based on how they're encountered.
    //It's only to be done when the list is actually needed.
    
    //A handwritten list of every enemy ID correlated to their name in the game.
    //Mostly for my sanity's sake.
    public static Dictionary<string, string> IDToEnemy = new Dictionary<string, string>{

        ["en001"] = "Woodmai",
        ["en002"] = "Kingmai",
        ["en003"] = "Raicho",
        ["en004"] = "Hedgehog",
        ["en005"] = "Diatrima",
        ["en006"] = "Firezard",
        ["en007"] = "Slaveimp",
        ["en008"] = "Snowzard",
        ["en009"] = "Nozuchi",
        ["en010"] = "Garauchi",
        ["en011"] = "Bigcap",
        ["en012"] = "Kingcap",
        ["en013"] = "Raven",
        ["en014"] = "Snowbird",
        ["en015"] = "Frilzard",
        ["en016"] = "Gorezard",
        ["en017"] = "Gemzard",
        ["en018"] = "Carbuncl",
        ["en019"] = "Windsnip",
        ["en020"] = "Hugecrab",
        ["en021"] = "Oldcrab",
        ["en022"] = "Snowsoul",
        ["en023"] = "Cube Gel",
        ["en024"] = "Red Gel",
        ["en025"] = "Blue Gel",
        ["en026"] = "Gold Gel",
        ["en027"] = "King Gel",
        ["en028"] = "Ladybug",
        ["en029"] = "Rainbug",
        ["en030"] = "Defender",
        ["en031"] = "Woodbat",
        ["en032"] = "Petaloid",
        ["en033"] = "Venomfly",
        ["en034"] = "Mandrake",
        ["en035"] = "Waspior",
        ["en036"] = "Roller",
        ["en037"] = "Redwood",
        ["en038"] = "Ebonail",
        ["en039"] = "Redhorn",
        ["en040"] = "Trihorn",
        ["en041"] = "Cocatris",
        ["en042"] = "Hypnowl",
        ["en043"] = "Addleowl",
        ["en044"] = "Actaeon",
        ["en045"] = "Gryphon",
        ["en046"] = "Evil Eye",
        ["en047"] = "Stir Eye",
        ["en048"] = "Big Moth",
        ["en049"] = "Mothlord",
        ["en050"] = "Crawler",
        ["en051"] = "Venombug",
        ["en052"] = "Crawlest",
        ["en053"] = "Sleipnir",
        ["en054"] = "Nitemare",
        ["en055"] = "Armorman",
        ["en056"] = "Bloodman",
        ["en057"] = "Deathman",
        ["en058"] = "Raptor",
        ["en059"] = "Riptor",
        ["en060"] = "Tortmail",
        ["en061"] = "Tortiron",
        ["en062"] = "Cactoid",
        ["en063"] = "Cactlord",
        ["en064"] = "Mystue",
        ["en065"] = "Nastue",
        ["en066"] = "Gigantue",
        ["en067"] = "Darksoar",
        ["en068"] = "Ebonwing",
        ["en069"] = "Steelgun",
        ["en070"] = "Beamedge",
        ["en071"] = "Fishman",
        ["en072"] = "Redfish",
        ["en073"] = "Hexgourd",
        ["en074"] = "Flygourd",
        ["en075"] = "Trigourd",
        ["en076"] = "Mole",
        ["en077"] = "Furyhorn",
        ["en078"] = "Wolf",
        ["en079"] = "Glowbird",
        ["en080"] = "Spider",
        ["en081"] = "Moa",
        ["en082"] = "Gigaboar",
        ["en083"] = "Moriyana",
        ["en084"] = "Clawbug",
        ["en085"] = "Muckdile",
        ["en086"] = "Killclaw",
        ["en087"] = "Killpion",
        ["en088"] = "Fangleaf",
        ["en089"] = "Stalker",
        ["en090"] = "Armoth",
        ["en091"] = "Sickwood",
        ["en092"] = "Warbull",
        ["en093"] = "Razeking",
        ["en094"] = "Asterios",
        ["en095"] = "Helldra",
        ["en096"] = "Evildra",
        ["en097"] = "Poseidon",
        ["en098"] = "Sauromar",
        ["en099"] = "Fireking",
        ["en100"] = "Iceking",
        ["en101"] = "Voltking",
        ["en102"] = "Raflesia",
        ["en103"] = "Wrathbud",
        ["en104"] = "Dinolich",
        ["en105"] = "Shelltor",
        ["eb001"] = "Chimaera",
        ["eb002"] = "Salamox",
        ["eb003"] = "Hellion",
        ["eb004"] = "Briareus",
        ["eb005"] = "Scylla",
        ["eb006"] = "Ur-Child",
        ["eb007"] = "Harpuia",
        ["eb008"] = "Colossus",
        ["eb009"] = "Overlord 1",
        ["eb010"] = "Overlord 2",
        ["eb011"] = "Golem",
        ["eb012"] = "Wyvern",
        ["eb013"] = "Wyrm",
        ["eb014"] = "Dragon",
        ["eb015"] = "Drake",
        ["eb016"] = "Artelind",
        ["eb017"] = "Wilhelm"
    };
    
    //For whatever reason, some enemy IDs are shared. 
    //This ties the overriden enID to a Codex entry as well, which creates a unique key.
    //My initial suspicion is that these are quest-exclusive enemies.
    //It's not a perfect assumption, but it's
    //not being changed even if that's incorrect.
    //
    //After typing this, my assumption is that the graphics engine
    //uses the en number to determine the sprite.

    public static Dictionary<string, (string, int)> IDToQuestEnemy = new Dictionary<string, (string, int)>
    {
        ["en001"] = ("Dummy", 0), //Dummy data repeated in the file
        ["en003"] = ("Castwing",0), //"Missing pet"
        ["en004"] = ("Fleehog", 82), //"Connoisseur of leather"
        ["en019"] = ("Illgaze", 87), //"The golden shadow"
        ["en022"] = ("Icefiend", 119), //Snowsoul FOE in area opened during "The Sleeping Duke"
        ["en030"] = ("Guardian", 110), //Weak FOEs that assist Colossus
        ["en032"] = ("Pollener", 120),//Petaloid FOEs in area opened during "The Volt King's Rampage"
        ["en038"] = ("Call Ape", 93), //"An ambassador's plea"
        ["en044"] = ("Kilohorn", 118), //Tree Key actaeon FOE in S2
        ["en045"] = ("Sonicker", 117), //Tree Key gryphon FOE in S4
        ["en049"] = ("Huelord", 94), //"Charity to monsters"
        ["en051"] = ("Invader", 92), //"Outpatient treatment"
        ["en053"] = ("Madsteps", 0), //"Beast gone berzerk", Codex #0
        ["en055"] = ("Sentinel", 0), // Removed enemy? Can't find a reference. Codex #0
        ["en058"] = ("Gashtor", 81), //"A cranky monster"
        ["en061"] = ("Tortevil", 91), //"Task for the tracker"
        ["en064"] = ("Spectre", 83), //"A long way down"
        ["en071"] = ("Killfish",84), //"A bitter end"
        ["en072"] = ("Mawfish", 85), //"Rescue operation"
        ["en076"] = ("Raidmole",116), //Tree Key mole FOE in S1
        ["en077"] = ("Furylord", 97), //"Find the missing guards"
        ["en078"] = ("Edgewolf", 88), //"Pride of the gunner"
        ["en084"] = ("Thiclaw", 86), //"The sun shines down"
        ["en087"] = ("Banepion", 90), //"Law and Order"
        ["en088"] = ("Greedbud", 95), //Spawn on 1F upon reaching 29F
        ["en092"] = ("Awebull", 89) //"Explorers! Heroes! Caterers!"
    };

    //This ties enIDs to the amount of EXP granted that I've calculated.
    public static Dictionary<string, int> IDToEXP = new Dictionary<string, int>
    {
        ["en007"] =  820,   //Slaveimp
        ["en022"] = 17250,  //Icefiend, Snowsoul FOEs in area opened during "The Sleeping Duke"
        ["en030"] = 3300,   //Guardian, Defender FOEs that supports Colossus.
        ["en032"] = 17600,  //Pollener, Petaloid FOEs in area opened during "The Volt King's Rampage"
        ["en037"] = 5650,   //Redwood
        ["en041"] = 13000,  //Cocatris
        ["en044"] = 16900,  //Kilohorn, Tree Key actaeon FOE in S2
        ["en045"] = 16600,  //Sonicker, Tree Key gryphon FOE in S4
        ["en047"] = 17500,  //Stir Eye
        ["en057"] = 15500,  //Deathman
        ["en058"] = 3240,   //Raptor
        ["en059"] = 20800,  //Riptor
        ["en067"] = 8400,   //Darksoar
        ["en070"] = 19350,  //Beamedge
        ["en073"] = 22750,  //Hexgourd
        ["en074"] = 9980,   //Flygourd
        ["en075"] = 3610,   //Trigourd
        ["en076"] = 16500,  //Raidmole, Tree Key mole FOE in S1
        ["en077"] = 1920,   //Furyhorn
        ["en086"] = 8500,   //Killclaw
        ["en089"] = 5800,   //Stalker
        ["en090"] = 8580,   //Armoth
        ["en093"] = 5470,   //Razeking
        ["en094"] = 18850,  //Asterios
        ["en095"] = 31100,  //Helldra
        ["en096"] = 32500,  //Evildra
        ["en099"] = 17600,  //Fireking
        ["en100"] = 35000,  //Iceking
        ["en101"] = 26000,  //Voltking
        ["en104"] = 20150,  //Dinolich
        ["en105"] = 7700,   //Shelltor
        ["eb002"] = 200000, //Salamox
        ["eb004"] = 370000, //Briareus
        ["eb011"] = 200000, //Golem
        ["eb012"] = 355000, //Wyvern
        ["eb013"] = 390000, //Wyrm
        ["eb014"] = 390000, //Dragon
        ["eb015"] = 390000  //Drake
        
    };



}