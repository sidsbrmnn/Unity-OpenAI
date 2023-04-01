﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MyBox;
using OpenAI;
using OpenAI.AiModels;
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
        public enum ModelName
        {
            GPT_3,
            GPT_3_5_TURBO,
            GPT_3_5_TURBO_0301,
            GPT_4,
            GPT_4_0314,
            ADA,
            ADA_CODE_SEARCH_CODE,
            ADA_CODE_SEARCH_TEXT,
            ADA_SEARCH_DOCUMENT,
            ADA_SEARCH_QUERY,
            ADA_SIMILARITY,
            ADA_2020_05_03,
            BABBAGE,
            BABBAGE_CODE_SEARCH_CODE,
            BABBAGE_CODE_SEARCH_TEXT,
            BABBAGE_SEARCH_DOCUMENT,
            BABBAGE_SEARCH_QUERY,
            BABBAGE_SIMILARITY,
            BABBAGE_2020_05_03,
            // CODE_CUSHMAN_001,
            // CODE_DAVINCI_002,
            // CODE_DAVINCI_EDIT_001,
            CODE_SEARCH_ADA_CODE_001,
            CODE_SEARCH_ADA_TEXT_001,
            CODE_SEARCH_BABBAGE_CODE_001,
            CODE_SEARCH_BABBAGE_TEXT_001,
            CURIE,
            CURIE_INSTRUCT_BETA,
            CURIE_SEARCH_DOCUMENT,
            CURIE_SEARCH_QUERY,
            CURIE_SIMILARITY,
            CURIE_2020_05_03,
            CUSHMAN_2020_05_03,
            DAVINCI,
            DAVINCI_IF_3_0_0,
            DAVINCI_INSTRUCT_BETA,
            DAVINCI_INSTRUCT_BETA_2_0_0,
            DAVINCI_SEARCH_DOCUMENT,
            DAVINCI_SEARCH_QUERY,
            DAVINCI_SIMILARITY,
            DAVINCI_2020_05_03,
            // IF_CURIE_V2,
            // IF_DAVINCI_V2,
            // IF_DAVINCI_3_0_0,
            TEXT_ADA_001,
            TEXT_ADA__001,
            TEXT_BABBAGE_001,
            TEXT_BABBAGE__001,
            TEXT_CURIE_001,
            TEXT_CURIE__001,
            TEXT_DAVINCI_001,
            TEXT_DAVINCI_002,
            TEXT_DAVINCI_003,
            TEXT_DAVINCI_EDIT_001,
            TEXT_DAVINCI_INSERT_001,
            TEXT_DAVINCI_INSERT_002,
            TEXT_DAVINCI__001,
            TEXT_EMBEDDING_ADA_002,
            TEXT_SEARCH_ADA_DOC_001,
            TEXT_SEARCH_ADA_QUERY_001,
            TEXT_SEARCH_BABBAGE_DOC_001,
            TEXT_SEARCH_BABBAGE_QUERY_001,
            TEXT_SEARCH_CURIE_DOC_001,
            TEXT_SEARCH_CURIE_QUERY_001,
            TEXT_SEARCH_DAVINCI_DOC_001,
            TEXT_SEARCH_DAVINCI_QUERY_001,
            TEXT_SIMILARITY_ADA_001,
            TEXT_SIMILARITY_BABBAGE_001,
            TEXT_SIMILARITY_CURIE_001,
            TEXT_SIMILARITY_DAVINCI_001,
            // WHISPER_1
        }
        
        public static readonly Dictionary<ModelName, string> ModelToString = new Dictionary<ModelName, string>()
        {            
            { ModelName.GPT_3, "text-davinci-003" },
            { ModelName.GPT_3_5_TURBO, "gpt-3.5-turbo" },
            { ModelName.GPT_3_5_TURBO_0301, "gpt-3.5-turbo-0301" },
            { ModelName.GPT_4, "gpt-4" },
            { ModelName.GPT_4_0314, "gpt-4-0314" },
            { ModelName.ADA, "ada" },
            { ModelName.ADA_CODE_SEARCH_CODE, "ada-code-search-code" },
            { ModelName.ADA_CODE_SEARCH_TEXT, "ada-code-search-text" },
            { ModelName.ADA_SEARCH_DOCUMENT, "ada-search-document" },
            { ModelName.ADA_SEARCH_QUERY, "ada-search-query" },
            { ModelName.ADA_SIMILARITY, "ada-similarity" },
            { ModelName.ADA_2020_05_03, "ada:2020-05-03" },
            { ModelName.BABBAGE, "babbage" },
            { ModelName.BABBAGE_CODE_SEARCH_CODE, "babbage-code-search-code" },
            { ModelName.BABBAGE_CODE_SEARCH_TEXT, "babbage-code-search-text" },
            { ModelName.BABBAGE_SEARCH_DOCUMENT, "babbage-search-document" },
            { ModelName.BABBAGE_SEARCH_QUERY, "babbage-search-query" },
            { ModelName.BABBAGE_SIMILARITY, "babbage-similarity" },
            { ModelName.BABBAGE_2020_05_03, "babbage:2020-05-03" },
            // { ModelName.CODE_CUSHMAN_001, "code-cushman-001" },
            // { ModelName.CODE_DAVINCI_002, "code-davinci-002" },
            // { ModelName.CODE_DAVINCI_EDIT_001, "code-davinci-edit-001" },
            { ModelName.CODE_SEARCH_ADA_CODE_001, "code-search-ada-code-001" },
            { ModelName.CODE_SEARCH_ADA_TEXT_001, "code-search-ada-text-001" },
            { ModelName.CODE_SEARCH_BABBAGE_CODE_001, "code-search-babbage-code-001" },
            { ModelName.CODE_SEARCH_BABBAGE_TEXT_001, "code-search-babbage-text-001" },
            { ModelName.CURIE, "curie" },
            { ModelName.CURIE_INSTRUCT_BETA, "curie-instruct-beta" },
            { ModelName.CURIE_SEARCH_DOCUMENT, "curie-search-document" },
            { ModelName.CURIE_SEARCH_QUERY, "curie-search-query" },
            { ModelName.CURIE_SIMILARITY, "curie-similarity" },
            { ModelName.CURIE_2020_05_03, "curie:2020-05-03" },
            { ModelName.CUSHMAN_2020_05_03, "cushman:2020-05-03" },
            { ModelName.DAVINCI, "davinci" },
            { ModelName.DAVINCI_IF_3_0_0, "davinci-if:3.0.0" },
            { ModelName.DAVINCI_INSTRUCT_BETA, "davinci-instruct-beta" },
            { ModelName.DAVINCI_INSTRUCT_BETA_2_0_0, "davinci-instruct-beta:2.0.0" },
            { ModelName.DAVINCI_SEARCH_DOCUMENT, "davinci-search-document" },
            { ModelName.DAVINCI_SEARCH_QUERY, "davinci-search-query" },
            { ModelName.DAVINCI_SIMILARITY, "davinci-similarity" },
            { ModelName.DAVINCI_2020_05_03, "davinci:2020-05-03" },
            // { ModelName.IF_CURIE_V2, "if-curie-v2" },
            // { ModelName.IF_DAVINCI_V2, "if-davinci-v2" },
            // { ModelName.IF_DAVINCI_3_0_0, "if-davinci:3.0.0" },
            { ModelName.TEXT_ADA_001, "text-ada-001" },
            { ModelName.TEXT_ADA__001, "text-ada:001" },
            { ModelName.TEXT_BABBAGE_001, "text-babbage-001" },
            { ModelName.TEXT_BABBAGE__001, "text-babbage:001" },
            { ModelName.TEXT_CURIE_001, "text-curie-001" },
            { ModelName.TEXT_CURIE__001, "text-curie:001" },
            { ModelName.TEXT_DAVINCI_001, "text-davinci-001" },
            { ModelName.TEXT_DAVINCI_002, "text-davinci-002" },
            { ModelName.TEXT_DAVINCI_003, "text-davinci-003" },
            { ModelName.TEXT_DAVINCI_EDIT_001, "text-davinci-edit-001" },
            { ModelName.TEXT_DAVINCI_INSERT_001, "text-davinci-insert-001" },
            { ModelName.TEXT_DAVINCI_INSERT_002, "text-davinci-insert-002" },
            { ModelName.TEXT_DAVINCI__001, "text-davinci:001" },
            { ModelName.TEXT_EMBEDDING_ADA_002, "text-embedding-ada-002" },
            { ModelName.TEXT_SEARCH_ADA_DOC_001, "text-search-ada-doc-001" },
            { ModelName.TEXT_SEARCH_ADA_QUERY_001, "text-search-ada-query-001" },
            { ModelName.TEXT_SEARCH_BABBAGE_DOC_001, "text-search-babbage-doc-001" },
            { ModelName.TEXT_SEARCH_BABBAGE_QUERY_001, "text-search-babbage-query-001" },
            { ModelName.TEXT_SEARCH_CURIE_DOC_001, "text-search-curie-doc-001" },
            { ModelName.TEXT_SEARCH_CURIE_QUERY_001, "text-search-curie-query-001" },
            { ModelName.TEXT_SEARCH_DAVINCI_DOC_001, "text-search-davinci-doc-001" },
            { ModelName.TEXT_SEARCH_DAVINCI_QUERY_001, "text-search-davinci-query-001" },
            { ModelName.TEXT_SIMILARITY_ADA_001, "text-similarity-ada-001" },
            { ModelName.TEXT_SIMILARITY_BABBAGE_001, "text-similarity-babbage-001" },
            { ModelName.TEXT_SIMILARITY_CURIE_001, "text-similarity-curie-001" },
            { ModelName.TEXT_SIMILARITY_DAVINCI_001, "text-similarity-davinci-001" },
            // { ModelName.WHISPER_1, "whisper-1" }
        };

        public static readonly Dictionary<string, bool> isChat = new Dictionary<string, bool>()
        {
            { "gpt-3.5-turbo", true },
            { "gpt-3.5-turbo-0301", true },
            { "gpt-4", true }
        };

        public enum Size
        {
            SMALL,
            MEDIUM,
            LARGE
        }

        public static readonly Dictionary<Size, string> SizeToString = new Dictionary<Size, string>()
        {
            { Size.SMALL, "256x256" },
            { Size.MEDIUM, "512x512" },
            { Size.LARGE, "1024x1024" }
        };

        public enum MessageRole
        {
            SYSTEM,
            ASSISTANT, 
            USER
        }

        public static readonly Dictionary<MessageRole, string> MessageRoleToString = new Dictionary<MessageRole, string>()
        {
            { MessageRole.SYSTEM, "system" },
            { MessageRole.ASSISTANT, "assistant" },
            { MessageRole.USER, "user" }
        };

        private Configuration config;
        private bool verbose = true;
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

        public OpenAiApi(Configuration config = null, bool verbose = true)
        {
            this.config = config;
            this.verbose = verbose;
        }

        public Configuration ActiveConfig => config ?? OpenAi.Configuration.GlobalConfig;

        public string ApiKey
        {
            get
            {
                if (OpenAi.Configuration.GlobalConfig == null || OpenAi.Configuration.GlobalConfig.ApiKey == "")
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
                if (OpenAi.Configuration.GlobalConfig == null || OpenAi.Configuration.GlobalConfig.Organization == "")
                {
                    OpenAi.Configuration.GlobalConfig = ReadConfigFromUserDirectory();
                }
                return ActiveConfig.Organization;
            }
        }
        
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
        
        public Task<CompletionResponse> TextCompletion(string prompt, ModelTypes.TextCompletion model=ModelTypes.TextCompletion.GPT_3, Callback<CompletionResponse> callback=null)
        {
            return Post(new CompletionRequest{prompt=prompt, model=model}, callback);
        }
        
        public Task<ChatCompletionResponse> ChatCompletion(Message[] messages, ModelTypes.ChatCompletion model=ModelTypes.ChatCompletion.GPT_4, Callback<ChatCompletionResponse> callback=null)
        {
            return Post(new ChatCompletionRequest{messages=messages, model=model}, callback);
        }
        
        public Task<CompletionResponse> Send(CompletionRequest request, Callback<CompletionResponse> callback=null)
        {
            return Post(request, callback);
        }
        
        public Task<ChatCompletionResponse> Send(ChatCompletionRequest request, Callback<ChatCompletionResponse> callback=null)
        {
            return Post(request, callback);
        }

        public Task<ImageGenerationResponse> Send(ImageGenerationRequest request, Callback<ImageGenerationResponse> callback=null)
        {
            return CreateImage(request, callback);
        }
        
        #endregion

        #region Images

        public Task<ImageGenerationResponse> CreateImage(string prompt, ImageSize size=ImageSize.SMALL, Callback<ImageGenerationResponse> callback=null)
        {
            return CreateImage(new ImageGenerationRequest { prompt=prompt, size=size }, callback);
        }

        public Task<ImageGenerationResponse> CreateImage(ImageGenerationRequest request, Callback<ImageGenerationResponse> callback=null)
        {
            callback ??= value => {  };
            var taskCompletion = new TaskCompletionSource<ImageGenerationResponse>();
            Callback<ImageGenerationResponse> callbackIntercept = async image =>
            {
                Texture2D[] textures = await GetAllImages(image);
                for (int i = 0; i < textures.Length; i++)
                {
                    ImageData data = image.data[i];

                    Texture2D texture = textures[i];
                    if (OpenAi.Configuration.SaveTempImages)
                    {
                        string num = i > 0 ? (" " + i) : "";
                        texture = AiUtils.Image.SaveImageToFile(request.prompt + num, texture, false, AiUtils.Image.TempImageDirectory);
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
        
        private Task<Texture2D[]> GetAllImages(ImageGenerationResponse aiImage)
        {
            List<Task<Texture2D>> getImageTasks = new List<Task<Texture2D>>{};
            
            for (int i = 0; i < aiImage.data.Length; i++)
            {
                ImageData data = aiImage.data[i];
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
        
        public Task<O> Post<I,O>(I request, Callback<O> completionCallback = null)
            where I : ModelRequest<I>
            where O : ModelResponse<O>, new()
        {
            if (verbose)
            {
                Debug.Log($"Open AI API - Request Sent: \"{request.ToJson()}\"");                
            }
            
            completionCallback ??= value => {  };

            (Task<O> task, Callback<O> taskCallback) = CallbackToTask(completionCallback);
            Runner.StartCoroutine(Post(request.Url, request.ToJson(), taskCallback));

            return task;
        }

        private IEnumerator Post<O>(string url, string body, Callback<O> completionCallback) where O : ModelResponse<O>, new()
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
            
            O response;
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                response = new O().FromJson(webRequest.downloadHandler.text);
                response.Result = webRequest.result;
                
                if (verbose)
                {
                    Debug.Log($"Open AI API - Request Successful: \"{JsonUtility.ToJson(JsonUtility.FromJson<O>(webRequest.downloadHandler.text), true)}\"");
                }
            }
            else
            {
                response = new O
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
                    "body: " + body.Take(1000) + "..." + ": \n\n" +
                    "result: " + request.result + ": \n\n" +
                    "response: " + request.downloadHandler.text);
            }
        }
    }
}
