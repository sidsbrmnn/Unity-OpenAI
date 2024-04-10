using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenAI;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenAi
{
    [Serializable]
    public class Configuration
    {
        public static string AuthFileDir => "/.openai";
        public static string AuthFilePath => "/.openai/auth.json";
        public static Configuration GlobalConfig = new Configuration("", "");
        public static bool SaveTempImages => true;
        public class GlobalConfigFormat
        {
            public string private_api_key;
            public string organization;
        }

        //Specific key - this is if you want to support multiple api keys or anything like that.
        [SerializeField] private string apiKey;
        [SerializeField] private string organization;

        public Configuration(string apiKey, string organization)
        {
            this.apiKey = apiKey;
            this.organization = organization;
        }

        public string ApiKey => apiKey;
        public string Organization => organization;
    }

    public class OpenAiApi
    {
        public enum Model
        {
            GPT_35_TURBO_INSTRUCT,
            BABBAGE_002,
            DAVINCI_002,
            CHAT_GPT = GPT_35_TURBO_INSTRUCT
        }

        public enum Size
        {
            SMALL,
            MEDIUM,
            LARGE
        }

        public static readonly Dictionary<Model, string> ModelToString = new Dictionary<Model, string>()
        {
            { Model.GPT_35_TURBO_INSTRUCT, "gpt-3.5-turbo-instruct" },
            { Model.BABBAGE_002, "babbage-002" },
            { Model.DAVINCI_002, "davinci-002" }
        };

        public static readonly Dictionary<Size, string> SizeToString = new Dictionary<Size, string>()
        {
            { Size.SMALL, "256x256" },
            { Size.MEDIUM, "512x512" },
            { Size.LARGE, "1024x1024" }
        };

        private Configuration config;
        private static CoroutineRunner runner;
        public static CoroutineRunner Runner {
            get
            {
                if (!runner)
                {
                    runner = GameObject.FindObjectOfType<CoroutineRunner>();
                }

                if (!runner)
                {
                    GameObject gameObject = new GameObject("Open AI Request Runner");
                    gameObject.AddComponent<CoroutineRunner>();
                    gameObject.hideFlags = HideFlags.HideInHierarchy;
                    runner = gameObject.GetComponent<CoroutineRunner>();
                }

                if (Application.isPlaying && runner)
                {
                    UnityEngine.Object.DontDestroyOnLoad(runner.gameObject);
                }

                return runner;
            }
        }

        public OpenAiApi()
        {
            config = null;
        }

        public OpenAiApi(Configuration config)
        {
            this.config = config;
        }

        public Configuration ActiveConfig => config ?? OpenAi.Configuration.GlobalConfig;

        public string ApiKey
        {
            get
            {
                if (ActiveConfig == null)
                {
                    OpenAi.Configuration.GlobalConfig = ReadConfigFromUserDirectory();
                }
                return ActiveConfig.ApiKey;
            }
        }

        public string Organization
        {
            get
            {
                if (ActiveConfig == null)
                {
                    OpenAi.Configuration.GlobalConfig = ReadConfigFromUserDirectory();
                }
                return ActiveConfig.Organization;
            }
        }

        // public static Configuration

        public static string ConfigFileDir => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + OpenAi.Configuration.AuthFileDir;
        public static string ConfigFilePath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + OpenAi.Configuration.AuthFilePath;

        public static Configuration ReadConfigFromUserDirectory()
        {
            try
            {
                string jsonConfig = File.ReadAllText(ConfigFilePath);
                var config = JsonUtility.FromJson<Configuration.GlobalConfigFormat>(jsonConfig);
                return new Configuration(config.private_api_key, config.organization);
            }
            catch (Exception exception) when (exception is DirectoryNotFoundException || exception is FileNotFoundException)
            {
                return new Configuration("", "");
            }
        }

        public static void SaveConfigToUserDirectory(Configuration config)
        {
            if (!Directory.Exists(ConfigFileDir)) { Directory.CreateDirectory(ConfigFileDir); }
            Configuration.GlobalConfigFormat globalConfig = new Configuration.GlobalConfigFormat
            {
                private_api_key = config.ApiKey,
                organization = config.Organization
            };
            string jsonConfig = JsonUtility.ToJson(globalConfig, true);
            File.WriteAllText(ConfigFilePath, jsonConfig);
        }

        public static void Configuration(string globalApiKey, string globalOrganization)
        {
            OpenAi.Configuration.GlobalConfig = new Configuration(globalApiKey, globalOrganization);
        }

        public delegate void Callback<T>(T response=default);

        #region Completions

        public Task<AiText> CreateCompletion(string prompt, Callback<AiText> callback=null)
        {
            return CreateCompletion(prompt, Model.GPT_35_TURBO_INSTRUCT, callback);
        }

        public Task<AiText> CreateCompletion(string prompt, Model model, Callback<AiText> callback=null)
        {
            string modelString = ModelToString[model];
            return CreateCompletion(prompt, modelString, callback);
        }

        public Task<AiText> CreateCompletion(string prompt, string model, Callback<AiText> callback=null)
        {

            AiText.Request request = new AiText.Request(prompt, model);
            return CreateCompletion(request, callback);
        }

        public Task<AiText> CreateCompletion(AiText.Request request, Callback<AiText> callback=null)
        {
            return Post(request, callback);
        }

        #endregion

        #region Images

        public Task<AiImage> CreateImage(string prompt, Callback<AiImage> callback=null)
        {
            return CreateImage(prompt, Size.SMALL, callback);
        }

        public Task<AiImage> CreateImage(string prompt, Size size, Callback<AiImage> callback=null)
        {
            string sizeString = SizeToString[size];
            return CreateImage(prompt, sizeString, callback);
        }

        public Task<AiImage> CreateImage(string prompt, string size, Callback<AiImage> callback=null)
        {

            AiImage.Request request = new AiImage.Request(prompt, size);
            return CreateImage(request, callback);
        }

        public Task<AiImage> CreateImage(AiImage.Request request, Callback<AiImage> callback=null)
        {
            callback ??= value => {  };
            var taskCompletion = new TaskCompletionSource<AiImage>();
            Callback<AiImage> callbackIntercept = async image =>
            {
                Texture2D[] textures = await GetAllImages(image);
                for (int i = 0; i < textures.Length; i++)
                {
                    AiImage.Data data = image.data[i];

                    Texture2D texture = textures[i];
                    if (OpenAi.Configuration.SaveTempImages)
                    {
                        string num = i > 0 ? (" " + i) : "";
                        texture = Utils.Image.SaveToFile(request.prompt + num, texture, false, Utils.Image.TempDirectory);
                    }

                    data.texture = texture;
                }
                callback(image);
                taskCompletion.SetResult(image);
            };
            Post(request, callbackIntercept);
            return taskCompletion.Task;
        }

        #endregion

        private Task<Texture2D[]> GetAllImages(AiImage aiImage)
        {
            List<Task<Texture2D>> getImageTasks = new List<Task<Texture2D>>{};

            for (int i = 0; i < aiImage.data.Length; i++)
            {
                AiImage.Data data = aiImage.data[i];
                if (data.url != "")
                {
                    getImageTasks.Add(GetImageFromUrl(data.url));
                }
            }

            return Task.WhenAll(getImageTasks);
        }

        private Task<Texture2D> GetImageFromUrl(string url)
        {
            (Task<Texture2D> task, Callback<Texture2D> callback) = CallbackToTask<Texture2D>();
            Runner.StartCoroutine(GetImageFromUrl(url, callback));
            return task;
        }

        static IEnumerator GetImageFromUrl(string url, Callback<Texture2D> callback) {
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
            yield return webRequest.SendWebRequest();
            Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
            callback(texture);
        }

        private static Tuple<Task<T>, Callback<T>> CallbackToTask<T>(Callback<T> callback=null)
        {
            var taskCompletion = new TaskCompletionSource<T>();
            callback ??= value => {  };
            Callback<T> wrappedCallback = value =>
            {
                taskCompletion.SetResult(value);
                callback(value);
            };

            return new Tuple<Task<T>, Callback<T>>(taskCompletion.Task, wrappedCallback);
        }

        private Task<T> Post<T,R>(R requestBody, Callback<T> completionCallback=null) where T : IRequestable<T>, new()
        {
            string url = new T().URL;
            string bodyString = JsonUtility.ToJson(requestBody);
            completionCallback ??= value => {  };

            (Task<T> task, Callback<T> taskCallback) = CallbackToTask(completionCallback);
            Runner.StartCoroutine(Post(url, bodyString, taskCallback));

            return task;
        }

        private IEnumerator Post<T>(string url, string body, Callback<T> completionCallback) where T : IRequestable<T>, new()
        {
            UnityWebRequest webRequest = new UnityWebRequest(url);

            Dictionary<string, string> headers = new Dictionary<string, string>()
            {
                { "Authorization", "Bearer " + ApiKey },
                { "Content-Type", "application/json" },
                { "OpenAI-Organization", Organization },
            };
            foreach (var entry in headers)
            {
                webRequest.SetRequestHeader(entry.Key, entry.Value);
            }

            byte[] bodyByteArray = System.Text.Encoding.UTF8.GetBytes(body);

            webRequest.uploadHandler = new UploadHandlerRaw(bodyByteArray);
            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.disposeUploadHandlerOnDispose = true;
            webRequest.disposeDownloadHandlerOnDispose = true;
            webRequest.method = UnityWebRequest.kHttpVerbPOST;

            yield return webRequest.SendWebRequest();

            LogRequestResult(body, webRequest);

            T response;
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                response = new T().FromJson(webRequest.downloadHandler.text);
                response.Result = webRequest.result;
            }
            else
            {
                response = new T
                {
                    Result = webRequest.result
                };
            }

            webRequest.Dispose();

            completionCallback(response);
        }

        private void LogRequestResult(string body, UnityWebRequest request)
        {
            switch (request.result)
            {
                case UnityWebRequest.Result.ConnectionError: Log(); break;
                case UnityWebRequest.Result.DataProcessingError: Log(); break;
                case UnityWebRequest.Result.ProtocolError: Log(); break;
            }

            void Log()
            {
                Debug.LogError(
                    "Method: " + request.method + "\n" +
                    "URL: " + request.uri + ": \n" +
                    "body: " + body.Take(10000) + "..." + ": \n\n" +
                    "result: " + request.result + ": \n\n" +
                    "response: " + request.downloadHandler.text);
            }
        }
    }


    public interface IRequestable<T>
    {
        string URL { get; }
        UnityWebRequest.Result Result { set; get; }
        T FromJson(string jsonString);
    }

    [Serializable]
    public class AiText : IRequestable<AiText>
    {
        public string URL => "https://api.openai.com/v1/completions";

        public string Text => choices.Length > 0 ? choices[0].text : default;

        [Serializable]
        public class Request
        {
            public string prompt;
            public string model;
            public int n;
            public float temperature;
            public int max_tokens;

            private const int defaultMaxTokens = 1000;

            public Request(string prompt, string model, int n=1, float temperature=.8f, int max_tokens=defaultMaxTokens)
            {
                this.prompt = prompt;
                this.model = model;
                this.temperature = temperature;
                this.n = n;
                this.max_tokens = max_tokens;
            }

            public Request(string prompt, OpenAiApi.Model model, int n=1, float temperature=.8f, int max_tokens=defaultMaxTokens)
            {
                this.prompt = prompt;
                this.model = OpenAiApi.ModelToString[model];
                this.temperature = temperature;
                this.n = n;
                this.max_tokens = max_tokens;
            }
        }

        [Serializable]
        public class Choice
        {
            public string text = "";
            public int index = 0;
            public string logprobs;
            public string finish_reason;
        }

        [Serializable]
        public class Usage
        {
            public int prompt_tokens;
            public int completion_tokens;
            public int total_tokens;
        }

        public string id;
        public string obj;
        public int created;
        public string model;
        public Choice[] choices = new Choice[] {};
        public Usage usage;

        public UnityWebRequest.Result Result { get; set; }

        public AiText()
        {
            choices = new [] { new Choice() };
        }

        public AiText FromJson(string jsonString)
        {
            jsonString = jsonString.Replace("\"object\":", "\"obj\":"); //Have to replace object since it's a reserved word.
            AiText aiText = JsonUtility.FromJson<AiText>(jsonString);
            foreach (var choice in aiText.choices)
            {
                choice.text = choice.text.Trim();
            }
            return aiText;
        }
    }

    [Serializable]
    public class AiImage : IRequestable<AiImage>
    {
        public string URL => "https://api.openai.com/v1/images/generations";

        public Texture2D Texture => data.Length > 0 ? data[0].texture : default;

            [Serializable]
        public class Request
        {
            public string prompt;
            public string size;
            public int n;

            public Request(string prompt, string size="256x256", int n=1)
            {
                this.prompt = prompt;
                this.size = size;
                this.n = n;
            }

            public Request(string prompt, OpenAiApi.Size size=OpenAiApi.Size.SMALL, int n=1)
            {
                this.prompt = prompt;
                this.size = OpenAiApi.SizeToString[size];
                this.n = n;
            }
        }

        [Serializable]
        public class Data
        {
            public string url;
            public Texture2D texture;
        }

        public int created;
        public Data[] data = new Data[]{};

        public UnityWebRequest.Result Result { get; set; }

        public AiImage()
        {
            data = new [] { new Data
                { url = "", texture = null }
            };
        }

        public AiImage FromJson(string jsonString)
        {
            return JsonUtility.FromJson<AiImage>(jsonString);
        }
    }
}
