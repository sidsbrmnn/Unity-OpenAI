﻿using System;
using System.Net.Mime;
using UnityEngine;
using OpenAi;
using TMPro;

namespace OpenAi
{
    public class OpenAiTextReplace : MonoBehaviour
    {
        public TextMeshProUGUI textMesh;
        [Multiline]
        public string prompt;
        public OpenAiApi.Model model;
        [Multiline]
        public string response;
        
        public async void ReplaceText()
        {
            OpenAiApi openai = new OpenAiApi(this);
            Completion completion = await openai.CreateCompletion(prompt, model);
            if (textMesh != null)
            {
                textMesh.text = completion.choices[0].text;
            }
            
            // openai.CreateCompletion(prompt, model, completion =>
            // {
            //     if (textMesh != null)
            //     {
            //         textMesh.text = completion.choices[0].text;
            //     }
            // });
        }
    }
}