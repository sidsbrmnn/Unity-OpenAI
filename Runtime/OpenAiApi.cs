﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace OpenAi
{
    [Serializable]
    public class Configuration
    {
        public static string AuthFilePath => "/.openai/auth.json";
        public static Configuration GlobalConfig;
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
            CHAT_GPT,
            ADA,
            ADA_CODE_SEARCH_CODE,
            ADA_CODE_SEARCH_TEXT,
            ADA_SEARCH_DOCUMENT,
            ADA_SEARCH_QUERY,
            ADA_SIMILARITY,
            ADA_2020_05_03,
            AUDIO_TRANSCRIBE_DEPRECATED,
            BABBAGE,
            BABBAGE_CODE_SEARCH_CODE,
            BABBAGE_CODE_SEARCH_TEXT,
            BABBAGE_SEARCH_DOCUMENT,
            BABBAGE_SEARCH_QUERY,
            BABBAGE_SIMILARITY,
            BABBAGE_2020_05_03,
            CODE_CUSHMAN_001,
            CODE_DAVINCI_002,
            CODE_DAVINCI_EDIT_001,
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
            IF_CURIE_V2,
            IF_DAVINCI_V2,
            IF_DAVINCI_3_0_0,
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
            TEXT_SIMILARITY_DAVINCI_001
        }

        public enum Size
        {
            SMALL, 
            MEDIUM, 
            LARGE
        }
        
        private Dictionary<Model, string> modelToString = new Dictionary<Model, string>()
        {
            { Model.CHAT_GPT, "text-davinci-003" },
            { Model.ADA, "ada" },
            { Model.ADA_CODE_SEARCH_CODE, "ada-code-search-code" },
            { Model.ADA_CODE_SEARCH_TEXT, "ada-code-search-text" },
            { Model.ADA_SEARCH_DOCUMENT, "ada-search-document" },
            { Model.ADA_SEARCH_QUERY, "ada-search-query" },
            { Model.ADA_SIMILARITY, "ada-similarity" },
            { Model.ADA_2020_05_03, "ada:2020-05-03" },
            { Model.AUDIO_TRANSCRIBE_DEPRECATED, "audio-transcribe-deprecated" },
            { Model.BABBAGE, "babbage" },
            { Model.BABBAGE_CODE_SEARCH_CODE, "babbage-code-search-code" },
            { Model.BABBAGE_CODE_SEARCH_TEXT, "babbage-code-search-text" },
            { Model.BABBAGE_SEARCH_DOCUMENT, "babbage-search-document" },
            { Model.BABBAGE_SEARCH_QUERY, "babbage-search-query" },
            { Model.BABBAGE_SIMILARITY, "babbage-similarity" },
            { Model.BABBAGE_2020_05_03, "babbage:2020-05-03" },
            { Model.CODE_CUSHMAN_001, "code-cushman-001" },
            { Model.CODE_DAVINCI_002, "code-davinci-002" },
            { Model.CODE_DAVINCI_EDIT_001, "code-davinci-edit-001" },
            { Model.CODE_SEARCH_ADA_CODE_001, "code-search-ada-code-001" },
            { Model.CODE_SEARCH_ADA_TEXT_001, "code-search-ada-text-001" },
            { Model.CODE_SEARCH_BABBAGE_CODE_001, "code-search-babbage-code-001" },
            { Model.CODE_SEARCH_BABBAGE_TEXT_001, "code-search-babbage-text-001" },
            { Model.CURIE, "curie" },
            { Model.CURIE_INSTRUCT_BETA, "curie-instruct-beta" },
            { Model.CURIE_SEARCH_DOCUMENT, "curie-search-document" },
            { Model.CURIE_SEARCH_QUERY, "curie-search-query" },
            { Model.CURIE_SIMILARITY, "curie-similarity" },
            { Model.CURIE_2020_05_03, "curie:2020-05-03" },
            { Model.CUSHMAN_2020_05_03, "cushman:2020-05-03" },
            { Model.DAVINCI, "davinci" },
            { Model.DAVINCI_IF_3_0_0, "davinci-if:3.0.0" },
            { Model.DAVINCI_INSTRUCT_BETA, "davinci-instruct-beta" },
            { Model.DAVINCI_INSTRUCT_BETA_2_0_0, "davinci-instruct-beta:2.0.0" },
            { Model.DAVINCI_SEARCH_DOCUMENT, "davinci-search-document" },
            { Model.DAVINCI_SEARCH_QUERY, "davinci-search-query" },
            { Model.DAVINCI_SIMILARITY, "davinci-similarity" },
            { Model.DAVINCI_2020_05_03, "davinci:2020-05-03" },
            { Model.IF_CURIE_V2, "if-curie-v2" },
            { Model.IF_DAVINCI_V2, "if-davinci-v2" },
            { Model.IF_DAVINCI_3_0_0, "if-davinci:3.0.0" },
            { Model.TEXT_ADA_001, "text-ada-001" },
            { Model.TEXT_ADA__001, "text-ada:001" },
            { Model.TEXT_BABBAGE_001, "text-babbage-001" },
            { Model.TEXT_BABBAGE__001, "text-babbage:001" },
            { Model.TEXT_CURIE_001, "text-curie-001" },
            { Model.TEXT_CURIE__001, "text-curie:001" },
            { Model.TEXT_DAVINCI_001, "text-davinci-001" },
            { Model.TEXT_DAVINCI_002, "text-davinci-002" },
            { Model.TEXT_DAVINCI_003, "text-davinci-003" },
            { Model.TEXT_DAVINCI_EDIT_001, "text-davinci-edit-001" },
            { Model.TEXT_DAVINCI_INSERT_001, "text-davinci-insert-001" },
            { Model.TEXT_DAVINCI_INSERT_002, "text-davinci-insert-002" },
            { Model.TEXT_DAVINCI__001, "text-davinci:001" },
            { Model.TEXT_EMBEDDING_ADA_002, "text-embedding-ada-002" },
            { Model.TEXT_SEARCH_ADA_DOC_001, "text-search-ada-doc-001" },
            { Model.TEXT_SEARCH_ADA_QUERY_001, "text-search-ada-query-001" },
            { Model.TEXT_SEARCH_BABBAGE_DOC_001, "text-search-babbage-doc-001" },
            { Model.TEXT_SEARCH_BABBAGE_QUERY_001, "text-search-babbage-query-001" },
            { Model.TEXT_SEARCH_CURIE_DOC_001, "text-search-curie-doc-001" },
            { Model.TEXT_SEARCH_CURIE_QUERY_001, "text-search-curie-query-001" },
            { Model.TEXT_SEARCH_DAVINCI_DOC_001, "text-search-davinci-doc-001" },
            { Model.TEXT_SEARCH_DAVINCI_QUERY_001, "text-search-davinci-query-001" },
            { Model.TEXT_SIMILARITY_ADA_001, "text-similarity-ada-001" },
            { Model.TEXT_SIMILARITY_BABBAGE_001, "text-similarity-babbage-001" },
            { Model.TEXT_SIMILARITY_CURIE_001, "text-similarity-curie-001" },
            { Model.TEXT_SIMILARITY_DAVINCI_001, "text-similarity-davinci-001" }
        };
            
        private Dictionary<Size, string> sizeToString = new Dictionary<Size, string>()
        {
            { Size.SMALL, "256x256" },
            { Size.MEDIUM, "512x512" },
            { Size.LARGE, "1024x1024" } 
        };
       
        private Configuration config;
        private MonoBehaviour monoBehaviour;

        public OpenAiApi(Configuration config, MonoBehaviour monoBehaviour)
        {
            this.config = config;
            this.monoBehaviour = monoBehaviour;
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

        private Configuration ReadConfigFromUserDirectory()
        {
            string configFilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + OpenAi.Configuration.AuthFilePath;
            string jsonConfig = File.ReadAllText(configFilePath);
            var config = JsonUtility.FromJson<Configuration.GlobalConfigFormat>(jsonConfig);
            return new Configuration(config.private_api_key, config.organization);
        }
        
        public static void Configuration(string globalApiKey, string globalOrganization)
        {
            OpenAi.Configuration.GlobalConfig = new Configuration(globalApiKey, globalOrganization);
        }
        public OpenAiApi(MonoBehaviour monoBehaviour)
        {
            config = null;
            this.monoBehaviour = monoBehaviour;
        }
        
        public delegate void Callback<T>(T response=default);

        #region Completions

        public Task<Completion> CreateCompletion(string prompt, Model model, Callback<Completion> callback=null)
        {
            string modelString = modelToString[model];
            return CreateCompletion(prompt, modelString, callback);
        }

        public Task<Completion> CreateCompletion(string prompt, string model, Callback<Completion> callback=null)
        {
            
            Completion.Request request = new Completion.Request(prompt, model);
            return CreateCompletion(request, callback);
        }

        public Task<Completion> CreateCompletion(Completion.Request request, Callback<Completion> callback=null)
        {
            return Post(request, callback);
        }
        
        #endregion

        #region Images

        public Task<Image> CreateImage(string prompt, Size size, Callback<Image> callback=null)
        {
            string sizeString = sizeToString[size];
            return CreateImage(prompt, sizeString, callback);
        }

        public Task<Image> CreateImage(string prompt, string size, Callback<Image> callback=null)
        {
            
            Image.Request request = new Image.Request(prompt, size);
            return CreateImage(request, callback);
        }

        public Task<Image> CreateImage(Image.Request request, Callback<Image> callback=null)
        {
            callback ??= value => {  };
            var taskCompletion = new TaskCompletionSource<Image>();
            Callback<Image> callbackIntercept = async image =>
            {
                Texture[] textures = await GetAllImages(image);
                for (int i = 0; i < textures.Length; i++)
                {
                    Image.Data data = image.data[i];
                    data.texture = textures[i];
                }
                callback(image);
                taskCompletion.SetResult(image);
            };
            Post(request, callbackIntercept);
            return taskCompletion.Task;
        }
        
        #endregion
        
        Task<Texture[]> GetAllImages(Image image)
        {
            Task<Texture>[] getImageTasks = new Task<Texture>[image.data.Length];
            
            for (int i = 0; i < image.data.Length; i++)
            {
                Image.Data data = image.data[i];
                getImageTasks[i] = GetImage(data.url);
            }

            return Task.WhenAll(getImageTasks);
        }

        private Task<Texture> GetImage(string url)
        {
            (Task<Texture> task, Callback<Texture> callback) = CallbackToTask<Texture>();
            monoBehaviour.StartCoroutine(GetImage(url, callback));
            return task;
        }

        IEnumerator GetImage(string url, Callback<Texture> callback) {
            UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url);
            yield return webRequest.SendWebRequest();
            Texture texture = DownloadHandlerTexture.GetContent(webRequest);
            callback(texture);
        }

        private Tuple<Task<T>, Callback<T>> CallbackToTask<T>(Callback<T> callback=null)
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
            monoBehaviour.StartCoroutine(Post(url, bodyString, taskCallback));

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
            webRequest.method = UnityWebRequest.kHttpVerbPOST;
            
            yield return webRequest.SendWebRequest();
            LogRequestResult(body, webRequest);

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                var parsedResponse = new T().FromJson(webRequest.downloadHandler.text);
                completionCallback(parsedResponse);
            }
            else
            {
                completionCallback();
            }
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
                    "body: " + body + ": \n\n" +
                    "result: " + request.result + ": \n\n" +
                    "response: " + request.downloadHandler.text);
            }
        }
    }
    
    public interface IRequestable<T>
    {
        string URL { get; }
        T FromJson(string jsonString);
    }
    
    [Serializable]
    public class Completion : IRequestable<Completion>
    {
        public string URL => "https://api.openai.com/v1/completions";
            
        [Serializable]
        public class Request
        {
            public string prompt;
            public string model;
            public int n;
            public float temperature;
            public int max_tokens;

            public Request(string prompt, string model, int n=1, float temperature=.8f, int max_tokens=100)
            {
                this.prompt = prompt;
                this.model = model;
                this.temperature = temperature;
                this.n = n;
                this.max_tokens = max_tokens;
            }
        }
            
        [Serializable]
        public class Choice
        {
            public string text;
            public int index;
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
        public Choice[] choices;
        public Usage usage;
            
        public Completion FromJson(string jsonString)
        {
            jsonString = jsonString.Replace("\"object\":", "\"obj\":"); //Have to replace object since it's a reserved word. 
            Completion completion = JsonUtility.FromJson<Completion>(jsonString);
            foreach (var choice in completion.choices)
            {
                choice.text = choice.text.Trim();
            }
            return completion;
        }
    }
    
    [Serializable]
    public class Image : IRequestable<Image>
    {
        public string URL => "https://api.openai.com/v1/images/generations";
            
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
        }
        
        [Serializable]
        public class Data
        {
            public string url;
            public Texture texture;
        }
        
        public int created;
        public Data[] data;
            
        public Image FromJson(string jsonString)
        {
            return JsonUtility.FromJson<Image>(jsonString);
        }
    }
}