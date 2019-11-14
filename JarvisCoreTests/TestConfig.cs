using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JarvisCoreTests
{
    [JsonObject]
    class TestConfig
    {
        [JsonProperty(PropertyName = "bot_token")]
        public string BotToken { get; set; }
        [JsonProperty(PropertyName = "global_admins")]
        public int[] GlobalAdmins { get; set; }
        [JsonProperty(PropertyName = "test_user")]
        public int TestUser { get; set; }
    }
}
