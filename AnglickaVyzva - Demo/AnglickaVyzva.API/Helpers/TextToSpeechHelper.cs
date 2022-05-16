using Google.Cloud.TextToSpeech.V1;
using System;
using System.IO;

namespace AnglickaVyzva.API.Helpers
{
    public class TextToSpeechHelper
    {
        public static TextToSpeechClient CreateTextToSpeechClient()
        {
            var path_googleApiServiceKey = @"./PrivateKey/privateKey-GoogleTextToSpeech.json";
            var privateKeyJson = File.ReadAllText(path_googleApiServiceKey);

            // Instantiate a client
            var builder = new TextToSpeechClientBuilder
            {
                JsonCredentials = privateKeyJson
            };
            var client = builder.Build();

            return client;
        }

        public static Stream DownloadAudio(TextToSpeechClient client, string text)
        {
            VoiceSelectionParams voice = new VoiceSelectionParams
            {
                //LanguageCode = "en-US",
                ////SsmlGender = SsmlVoiceGender.Male,
                //Name = "en-US-Wavenet-D",
                ////Name = "en-US-Wavenet-E",


                LanguageCode = "de-DE",
                Name = "de-DE-Wavenet-D",
            };

            // Select the type of audio file you want returned.
            AudioConfig config = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Mp3
            };

            // Set the text input to be synthesized.
            SynthesisInput input = new SynthesisInput
            {
                Text = text
            };

            // Perform the Text-to-Speech request, passing the text input
            // with the selected voice parameters and audio file type
            var response = client.SynthesizeSpeech(new SynthesizeSpeechRequest
            {
                Input = input,
                Voice = voice,
                AudioConfig = config
            });

            var stream = new MemoryStream();
            response.AudioContent.WriteTo(stream);

            return stream;
        }
    }
}
