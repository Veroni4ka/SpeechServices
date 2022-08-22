// See https://aka.ms/new-console-template for more information
using SpeechServices;
using System;

string choose = " Please choose one of the following samples:";
string mainPrompt = " Your choice (or 0 to exit): ";
string exiting = "\n Exiting...";
string invalid = "\n Invalid input, choose again.";
//string done = "\n Done!";

ConsoleKeyInfo x;

Console.WriteLine("");
Console.WriteLine(" Speech SDK - Speech Recognition Samples");
Console.WriteLine("");
Console.WriteLine(choose);
Console.WriteLine("");
Console.WriteLine(" 1. Speech recognition with microphone input.");
Console.WriteLine(" 2. Translation with microphone input.");
Console.WriteLine(" 3. Translation with language detection.");
Console.WriteLine(" 4. Speech with intent recognition with microphone input.");
Console.WriteLine(" 5. Speech sythesys using specified voice.");
Console.WriteLine();
Console.WriteLine(mainPrompt);


x = Console.ReadKey();
Console.WriteLine("");

Utilities.Setup();

switch (x.Key)
{
    //
    case ConsoleKey.D1:
    case ConsoleKey.NumPad1:
        Utilities.SpeechRecognitionAsync().Wait();
        break;
    case ConsoleKey.D2:
    case ConsoleKey.NumPad2:
        Utilities.SpeechTranslationFromFile().Wait();
        break;
    case ConsoleKey.D3:
    case ConsoleKey.NumPad3:
        Utilities.TranslationWithLanguageDetectionAsync().Wait();
        break;
    //
    case ConsoleKey.D4:
    case ConsoleKey.NumPad4:
        Utilities.SpeechWithIntentRecognitionAsync().Wait();
        break;
    //
    case ConsoleKey.D5:
    case ConsoleKey.NumPad5:
        Utilities.SynthesisWithVoiceAsync().Wait();
        break;
    case ConsoleKey.D0:
    case ConsoleKey.NumPad0:
        Console.WriteLine(exiting);
        break;
    default:
        Console.WriteLine(invalid);
        break;

}
