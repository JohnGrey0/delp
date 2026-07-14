namespace Delp.Core.Tools.DevUtilities;

/// <summary>
/// Embedded reference data for <see cref="MockDataTool"/>. All arrays are
/// hand-curated, quality, real-ish data — no offensive or placeholder-junk
/// entries — sized per docs/TOOLSPEC.md's Batch L minimums.
/// </summary>
public static class MockCorpus
{
    public static readonly string[] FirstNames =
    {
        "James", "Mary", "John", "Patricia", "Robert", "Jennifer", "Michael", "Linda",
        "William", "Elizabeth", "David", "Barbara", "Richard", "Susan", "Joseph", "Jessica",
        "Thomas", "Sarah", "Charles", "Karen", "Christopher", "Nancy", "Daniel", "Lisa",
        "Matthew", "Betty", "Anthony", "Margaret", "Mark", "Sandra", "Donald", "Ashley",
        "Steven", "Kimberly", "Andrew", "Emily", "Paul", "Donna", "Joshua", "Michelle",
        "Kenneth", "Carol", "Kevin", "Amanda", "Brian", "Melissa", "George", "Deborah",
        "Edward", "Stephanie", "Ronald", "Rebecca", "Timothy", "Sharon", "Jason", "Laura",
        "Jeffrey", "Cynthia", "Ryan", "Kathleen", "Jacob", "Amy", "Gary", "Angela",
        "Nicholas", "Shirley", "Eric", "Anna", "Jonathan", "Brenda", "Stephen", "Pamela",
        "Larry", "Emma", "Justin", "Nicole", "Scott", "Helen", "Brandon", "Samantha",
        "Benjamin", "Katherine", "Samuel", "Christine", "Gregory", "Debra", "Frank", "Rachel",
        "Alexander", "Carolyn", "Raymond", "Janet", "Patrick", "Maria", "Jack", "Olivia",
        "Dennis", "Heather", "Jerry", "Diane", "Priya", "Ravi", "Arjun", "Meera",
        "Wei", "Ming", "Yuki", "Haruto", "Sofia", "Mateo", "Diego", "Lucia",
        "Fatima", "Omar", "Youssef", "Amara", "Kwame", "Zanele", "Chidi", "Ngozi",
        "Ivan", "Olga", "Dmitri", "Katarina", "Hans", "Greta", "Liam", "Noah",
        "Ava", "Mia", "Ethan", "Isabella", "Lucas", "Chloe", "Nadia", "Tariq",
        "Layla", "Amir", "Zara", "Aisha", "Malik",
    };

    public static readonly string[] LastNames =
    {
        "Smith", "Johnson", "Williams", "Brown", "Jones", "Garcia", "Miller", "Davis",
        "Rodriguez", "Martinez", "Hernandez", "Lopez", "Gonzalez", "Wilson", "Anderson", "Thomas",
        "Taylor", "Moore", "Jackson", "Martin", "Lee", "Perez", "Thompson", "White",
        "Harris", "Sanchez", "Clark", "Ramirez", "Lewis", "Robinson", "Walker", "Young",
        "Allen", "King", "Wright", "Scott", "Torres", "Nguyen", "Hill", "Flores",
        "Green", "Adams", "Nelson", "Baker", "Hall", "Rivera", "Campbell", "Mitchell",
        "Carter", "Roberts", "Gomez", "Phillips", "Evans", "Turner", "Diaz", "Parker",
        "Cruz", "Edwards", "Collins", "Reyes", "Stewart", "Morris", "Morales", "Murphy",
        "Cook", "Rogers", "Gutierrez", "Ortiz", "Morgan", "Cooper", "Peterson", "Bailey",
        "Reed", "Kelly", "Howard", "Ramos", "Kim", "Cox", "Ward", "Richardson",
        "Watson", "Brooks", "Chavez", "Wood", "James", "Bennett", "Gray", "Mendoza",
        "Ruiz", "Hughes", "Price", "Alvarez", "Castillo", "Sanders", "Patel", "Myers",
        "Long", "Ross", "Foster", "Jimenez", "Powell", "Jenkins", "Perry", "Russell",
        "Sullivan", "Bell", "Coleman", "Butler", "Henderson", "Barnes", "Gonzales", "Fisher",
        "Vasquez", "Simmons", "Romero", "Jordan", "Patterson", "Alexander", "Hamilton", "Graham",
        "Reynolds", "Griffin", "Wallace", "Moreno", "West", "Cole", "Hayes", "Chen",
        "Wang", "Zhang", "Kumar", "Singh", "Sharma", "Khan", "Ahmed", "Ali",
        "Ibrahim", "Mohamed", "Muller", "Schmidt", "Fischer", "Weber", "Meyer", "Wagner",
        "Becker", "Hoffmann", "Schulz", "Nakamura", "Sato", "Suzuki", "Takahashi", "Kobayashi",
        "Yamamoto", "Novak", "Dvorak", "Kowalski", "Nowak", "Ivanov", "Petrov", "Popov",
    };

    public static readonly string[] Cities =
    {
        "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego",
        "Dallas", "Austin", "San Jose", "Fort Worth", "Jacksonville", "Columbus", "Charlotte", "San Francisco",
        "Indianapolis", "Seattle", "Denver", "Washington", "Boston", "Nashville", "Detroit", "Portland",
        "Las Vegas", "Memphis", "Louisville", "Baltimore", "Milwaukee", "Albuquerque", "Tucson", "Fresno",
        "Sacramento", "Kansas City", "Atlanta", "Omaha", "Raleigh", "Miami", "Oakland", "Minneapolis",
        "Tulsa", "Tampa", "Arlington", "New Orleans", "Toronto", "Vancouver", "Montreal", "London",
        "Manchester", "Paris", "Berlin", "Munich", "Madrid", "Barcelona", "Rome", "Milan",
        "Amsterdam", "Dublin", "Vienna", "Lisbon", "Prague", "Warsaw", "Zurich", "Oslo",
        "Stockholm", "Helsinki", "Athens", "Sydney", "Melbourne", "Auckland", "Tokyo", "Osaka",
        "Seoul", "Singapore", "Mumbai", "Delhi", "Bangalore", "Cairo", "Nairobi", "Johannesburg",
    };

    public static readonly string[] UsStates =
    {
        "Alabama", "Alaska", "Arizona", "Arkansas", "California",
        "Colorado", "Connecticut", "Delaware", "Florida", "Georgia",
        "Hawaii", "Idaho", "Illinois", "Indiana", "Iowa",
        "Kansas", "Kentucky", "Louisiana", "Maine", "Maryland",
        "Massachusetts", "Michigan", "Minnesota", "Mississippi", "Missouri",
        "Montana", "Nebraska", "Nevada", "New Hampshire", "New Jersey",
        "New Mexico", "New York", "North Carolina", "North Dakota", "Ohio",
        "Oklahoma", "Oregon", "Pennsylvania", "Rhode Island", "South Carolina",
        "South Dakota", "Tennessee", "Texas", "Utah", "Vermont",
        "Virginia", "Washington", "West Virginia", "Wisconsin", "Wyoming",
    };

    public static readonly string[] Countries =
    {
        "United States", "Canada", "United Kingdom", "Germany", "France", "Italy", "Spain", "Portugal",
        "Netherlands", "Belgium", "Switzerland", "Austria", "Sweden", "Norway", "Denmark", "Finland",
        "Ireland", "Poland", "Greece", "Japan", "South Korea", "China", "India", "Australia",
        "New Zealand", "Brazil", "Mexico", "Argentina", "South Africa", "Egypt",
    };

    public static readonly string[] CompanyNouns =
    {
        "Nova", "Vertex", "Quantum", "Lumina", "Echo", "Atlas", "Nimbus", "Cobalt",
        "Orbit", "Zephyr", "Granite", "Solstice", "Pulse", "Harbor", "Summit", "Cascade",
        "Ember", "Meridian", "Onyx", "Falcon", "Terra", "Vista", "Crest", "Aurora",
        "Halcyon", "Ridge", "Beacon", "Fusion", "Catalyst", "Horizon", "Sable", "Juniper",
        "Wren", "Talon", "Frost", "Cinder", "Marble", "Slate", "Amber", "Coral",
        "Delta", "Echelon", "Fathom", "Glacier", "Ironwood", "Basalt", "Comet", "Drift",
        "Gale", "Haven",
    };

    public static readonly string[] CompanyPatterns =
    {
        "{0} Labs", "{0} Inc.", "{0} Group", "{0} Technologies",
        "{0} & Co.", "{0} Solutions", "{0} Systems", "{0} Partners",
        "{0} Works", "{0} Studio", "{0}Soft", "{0} Dynamics",
        "{0} Ventures", "{0} Holdings", "{0} Industries", "{0} Networks",
        "{0} Analytics", "{0} Robotics", "{0} Biotech", "{0} Foods",
        "{0} Media", "{0} Capital", "{0} Logistics", "{0} Energy",
        "{0} Materials", "{0} Interactive", "{0} Digital", "{0} Global",
        "{0} Innovations", "{0} Consulting", "{0} Design", "{0} Software",
        "{0} Security", "{0} Health", "{0} Aerospace", "{0} Marine",
        "{0} Motors", "{0} Foundry", "{0} Collective", "{0} Exchange",
        "{0} Freight", "{0} Apparel", "{0} Publishing", "{0} Realty",
        "{0} Ventures Group",
    };

    public static readonly string[] JobTitles =
    {
        "Software Engineer", "Product Manager", "Data Analyst",
        "UX Designer", "Marketing Manager", "Sales Representative",
        "Account Executive", "Operations Manager", "Financial Analyst",
        "HR Coordinator", "Customer Success Manager", "DevOps Engineer",
        "QA Engineer", "Business Analyst", "Project Manager",
        "Content Strategist", "Graphic Designer", "Systems Administrator",
        "Network Engineer", "Security Analyst", "Data Scientist",
        "Machine Learning Engineer", "Technical Writer", "Recruiter",
        "Office Manager", "Executive Assistant", "Legal Counsel",
        "Accountant", "Controller", "Chief Executive Officer",
        "Chief Technology Officer", "Chief Financial Officer", "Support Specialist",
        "Solutions Architect", "Site Reliability Engineer", "Product Designer",
        "Research Scientist", "Supply Chain Manager", "Warehouse Supervisor",
        "Store Manager",
    };

    public static readonly string[] StreetNames =
    {
        "Maple", "Oak", "Cedar", "Elm", "Pine", "Birch", "Willow", "Chestnut",
        "Walnut", "Spruce", "Magnolia", "Sycamore", "Poplar", "Aspen", "Redwood", "Hickory",
        "Dogwood", "Beech", "Laurel", "Cypress", "Fir", "Larch", "Alder", "Hazel",
        "Linden", "Sunset", "River", "Lake", "Hill", "Meadow", "Park", "Highland",
        "Forest", "Ridge", "Valley", "Creek", "Spring", "Union", "Church",
    };
    public static readonly string[] StreetTypes =
    {
        "St", "Ave", "Blvd", "Ln", "Dr", "Rd", "Ct", "Way", "Pl", "Ter",
    };

    public static readonly string[] EmailDomains =
    {
        "gmail.com", "yahoo.com", "outlook.com", "hotmail.com", "icloud.com",
        "protonmail.com", "aol.com", "live.com", "mail.com", "fastmail.com",
        "zoho.com", "gmx.com", "yandex.com", "example.com", "workmail.io",
    };

    /// <summary>Short filler words used by the LoremWords field kind and mock URL paths.</summary>
    public static readonly string[] LoremWords =
    {
        "data", "alpha", "bridge", "cloud", "delta", "echo", "forest", "garden",
        "harbor", "island", "jungle", "kernel", "lantern", "meadow", "nimbus", "orbit",
        "pixel", "quartz", "river", "summit", "tundra", "umbra", "valley", "willow",
        "xenon", "yonder", "zephyr", "amber", "breeze", "crystal", "dawn", "ember",
        "flux", "glow", "horizon", "ivory", "jewel", "keystone", "lumen", "mist",
    };
}
