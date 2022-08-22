using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Intent;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Newtonsoft.Json.Linq;

namespace SpeechServices
{
    public sealed class Settings
    {
        public string? Key { get; set; }
        public string? Region { get; set; }

        public string? LUISId { get; set; }
        public string? CogKey { get; set; }
    }

    public class Utilities
    {
        private static Settings settings { get; set; }

        public static void Setup()
        {
            IConfiguration config = new ConfigurationBuilder()
                                        .AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json"), optional: false, reloadOnChange: false)
                                        .Build();
            settings = config.GetRequiredSection("Settings").Get<Settings>();
        }
        public static async Task SynthesisWithVoiceAsync()
        {
            var config = SpeechConfig.FromSubscription(settings.Key, settings.Region);

            //config.SpeechSynthesisLanguage = "en-US";
            //config.SpeechSynthesisVoiceName = "en-US-JennyMultilingualNeural";
            var voice = "Microsoft Server Speech Text to Speech Voice (en-US, JennyNeural)";
            config.SpeechSynthesisVoiceName = voice;

            using (var synthesizer = new SpeechSynthesizer(config))
            {
                while (true)
                {
                    Console.WriteLine("Enter some text that you want to speak, or enter empty text to exit.");
                    Console.Write("> ");
                    string text = Console.ReadLine();
                    if (string.IsNullOrEmpty(text))
                    {
                        break;
                    }

                    using (var result = await synthesizer.SpeakTextAsync(text))
                    {
                        if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                        {
                            Console.WriteLine($"Speech synthesized to speaker for text [{text}] with voice [{voice}]");
                        }
                        else if (result.Reason == ResultReason.Canceled)
                        {
                            var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                            Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                            if (cancellation.Reason == CancellationReason.Error)
                            {
                                Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                                Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                                Console.WriteLine($"CANCELED: Did you update the subscription info?");
                            }
                        }
                    }
                }
            }
        }

        public static async Task SpeechWithIntentRecognitionAsync()
        {
            var config = SpeechConfig.FromSubscription(settings.Key, settings.Region);
            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);

            // Creates a speech recognizer.
            using (var recognizer = new IntentRecognizer(config))
            {
                Console.WriteLine("Say something...");

                var model = LanguageUnderstandingModel.FromAppId(settings.LUISId);
                recognizer.AddAllIntents(model);
                
                Console.WriteLine(recognizer.Properties.GetProperty(PropertyId.SpeechServiceConnection_IntentRegion));

                var result = await recognizer.RecognizeOnceAsync();

                // Checks result.
                if (result.Reason == ResultReason.RecognizedIntent)
                {
                    Console.WriteLine($"RECOGNIZED: Text={result.Text}");
                    Console.WriteLine($"    Intent Id: {result.IntentId}.");
                    Console.WriteLine($"    Language Understanding JSON: {result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult)}.");
                    if (result.IntentId == "Translate")
                    {
                        var luisJson = JObject.Parse(result.Properties.GetProperty(PropertyId.LanguageUnderstandingServiceResponse_JsonResult));
                        string targetLng = luisJson["entities"].First(x => x["type"].ToString().ToLower() == "language")["entity"].ToString();
                        string text = luisJson["entities"].First(x => x["type"].ToString().ToLower() == "text")["entity"].ToString();

                        var lng = allCultures.FirstOrDefault(c => c.DisplayName.ToLower() == targetLng.ToLower()) ??
                                  allCultures.FirstOrDefault(c => c.DisplayName.ToLower() == "english");
                        var translated = Translate.TranslateText(lng.TwoLetterISOLanguageName, text, settings.CogKey);

                        Console.WriteLine("Translation: " + translated);

                        var synth = new System.Speech.Synthesis.SpeechSynthesizer();

                        // Configure the audio output.   
                        synth.SetOutputToDefaultAudioDevice();

                        // Speak a string.  
                        synth.SelectVoice(synth.GetInstalledVoices()
                                               .First(x => x.VoiceInfo.Culture.TwoLetterISOLanguageName == lng.TwoLetterISOLanguageName).VoiceInfo.Name);
                        synth.Speak(translated);
                    }
                }
                else if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"RECOGNIZED: Text={result.Text}");
                    Console.WriteLine($"    Intent not recognized.");
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                }
            }
        }

        public static async Task TranslationWithLanguageDetectionAsync()
        {
            string fromLanguage = "en-US";

            const string GermanVoice = "Microsoft Server Speech Text to Speech Voice (de-DE, Hedda)";

            var config = SpeechTranslationConfig.FromSubscription(settings.Key, settings.Region);

            config.SpeechRecognitionLanguage = fromLanguage;
            config.VoiceName = GermanVoice;

            config.AddTargetLanguage("de");

            config.SetProperty(PropertyId.SpeechServiceConnection_SingleLanguageIdPriority, "Latency");
            var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "en-US", "de-DE", "es-ES" });

            // Creates a translation recognizer using microphone as audio input.
            using (var recognizer = new TranslationRecognizer(config, autoDetectSourceLanguageConfig))
            {
                recognizer.Recognizing += (s, e) =>
                {
                    Console.WriteLine($"RECOGNIZING Text={e.Result.Text}");
                    foreach (var element in e.Result.Translations)
                    {
                        Console.WriteLine($" TRANSLATING into '{element.Key}': {element.Value}");
                    }
                };

                recognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.TranslatedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED Text={e.Result.Text}");
                        foreach (var element in e.Result.Translations)
                        {
                            Console.WriteLine($"    TRANSLATED into '{element.Key}': {element.Value}");
                        }
                    }
                    else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                        Console.WriteLine($"    Speech not translated.");
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                recognizer.Synthesizing += async (s, e) =>
                {
                    var audio = e.Result.GetAudio();
                    Console.WriteLine(audio.Length != 0
                        ? $"AudioSize: {audio.Length}"
                        : $"AudioSize: {audio.Length} (end of synthesis data)");

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        Console.WriteLine("\nSession started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("\nSession stopped event.");
                    };

                    Console.WriteLine("Say something...");
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    do
                    {
                        Console.WriteLine("Press Enter to stop");
                    } while (Console.ReadKey().Key != ConsoleKey.Enter);

                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                };
            }
        }

        public static async Task SpeechTranslationFromFile()
        {
            var v2EndpointInString = String.Format("wss://{0}.stt.speech.microsoft.com/speech/universal/v2", settings.Region);
            var v2EndpointUrl = new Uri(v2EndpointInString);

            var config = SpeechTranslationConfig.FromEndpoint(v2EndpointUrl, settings.Key);

            string fromLanguage = "en-US";
            config.SpeechRecognitionLanguage = fromLanguage;

            config.AddTargetLanguage("de");
            config.AddTargetLanguage("fr");

            config.SetProperty(PropertyId.SpeechServiceConnection_ContinuousLanguageIdPriority, "Latency");
            var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "en-US", "zh-CN" });

            var stopTranslation = new TaskCompletionSource<int>();

            using (var audioInput = AudioConfig.FromWavFileInput(@"en-us_zh-cn.wav"))
            {
                using (var recognizer = new TranslationRecognizer(config, autoDetectSourceLanguageConfig, audioInput))
                {
                    recognizer.Recognizing += (s, e) =>
                    {
                        var lidResult = e.Result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);

                        Console.WriteLine($"RECOGNIZING in '{lidResult}': Text={e.Result.Text}");
                        foreach (var element in e.Result.Translations)
                        {
                            Console.WriteLine($"    TRANSLATING into '{element.Key}': {element.Value}");
                        }
                    };

                    recognizer.Recognized += (s, e) => {
                        if (e.Result.Reason == ResultReason.TranslatedSpeech)
                        {
                            var lidResult = e.Result.Properties.GetProperty(PropertyId.SpeechServiceConnection_AutoDetectSourceLanguageResult);

                            Console.WriteLine($"RECOGNIZED in '{lidResult}': Text={e.Result.Text}");
                            foreach (var element in e.Result.Translations)
                            {
                                Console.WriteLine($"    TRANSLATED into '{element.Key}': {element.Value}");
                            }
                        }
                        else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");
                            Console.WriteLine($"    Speech not translated.");
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"CANCELED: Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                            Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                            Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }

                        stopTranslation.TrySetResult(0);
                    };

                    recognizer.SpeechStartDetected += (s, e) => {
                        Console.WriteLine("\nSpeech start detected event.");
                    };

                    recognizer.SpeechEndDetected += (s, e) => {
                        Console.WriteLine("\nSpeech end detected event.");
                    };

                    recognizer.SessionStarted += (s, e) => {
                        Console.WriteLine("\nSession started event.");
                    };

                    recognizer.SessionStopped += (s, e) => {
                        Console.WriteLine("\nSession stopped event.");
                        Console.WriteLine($"\nStop translation.");
                        stopTranslation.TrySetResult(0);
                    };

                    Console.WriteLine("Start translation...");
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    Task.WaitAny(new[] { stopTranslation.Task });

                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);
                }
            }
        }

    public static async Task SpeechRecognitionAsync()
        {
            var config = SpeechConfig.FromSubscription(settings.Key, settings.Region);

            using (var recognizer = new SpeechRecognizer(config))
            {
                // Starts recognizing.
                Console.WriteLine("Say something...");

                var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

                // Checks result.
                if (result.Reason == ResultReason.RecognizedSpeech)
                {
                    Console.WriteLine($"RECOGNIZED: Text={result.Text}");
                }
                else if (result.Reason == ResultReason.NoMatch)
                {
                    Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                }
                else if (result.Reason == ResultReason.Canceled)
                {
                    var cancellation = CancellationDetails.FromResult(result);
                    Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                    if (cancellation.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={cancellation.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you update the subscription info?");
                    }
                }
            }
        }
    }
}
