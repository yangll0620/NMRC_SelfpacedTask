using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SelfpacedTask
{
    class Config
    {
        [JsonProperty(PropertyName = "NHP Name")]
        public string NHPName;

        [JsonProperty(PropertyName = "saved folder")]
        public string saved_folder;

        [JsonProperty(PropertyName = "Times")]
        public ConfigTimes configTimes;


        [JsonProperty(PropertyName = "Colors")]
        public ConfigColors configColors;


        public string audioFile_Correct, audioFile_Error;
    }

    class ConfigTimes
    {

        [JsonProperty(PropertyName = "Max Reaction Time")]
        public float tMax_ReactionTime;

        [JsonProperty(PropertyName = "Max Reach Time")]
        public float tMax_ReachTime;

        [JsonProperty(PropertyName = "Visual Feedback Show Time")]
        public float t_VisfeedbackShow;

        [JsonProperty(PropertyName = "Inter Trials Time")]
        public float t_InterTrial;

        [JsonProperty(PropertyName = "Juice Correct Given Time")]
        public float t_JuicerCorrectGiven;
    }



    class ConfigColors
    {

        [JsonProperty(PropertyName = "Wait Start Interface Background")]
        public string BKWaitTrialColorStr;


        [JsonProperty(PropertyName = "Go Interface Background")]
        public string BKGoInterfaceColorStr;


        [JsonProperty(PropertyName = "Correct Feedback Interface Border")]
        public string CorrBDColorStr;


        [JsonProperty(PropertyName = "Error Feedback Interface Border")]
        public string ErrorBDColorStr;

    }
}