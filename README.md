# JarvisButlerBot
A modular telegram bot and group butler that aims to understand your text messages.

## Setup
Download a release (TODO) or build the source yourself, execute the JarvisButlerBot.exe and enter your Telegram Bot Token.

To install a module, copy the modules output dll file into the ```%APPDATA%\Crazypokemondev\JarvisButlerBot\modules``` folder and put all its dependency dll files into the ```%APPDATA%\Crazypokemondev\JarvisButlerBot\lib``` folder.

## Developing a module
Create a .NET Framework Class Library project (currently, JARVIS runs on .NET Framework 4.7.2). 

Install JarvisButlerBot.ModuleCore from NuGet.

In the library project, create a public class anywhere you like. Annotate it with the JarvisModuleAttribute and have it derive  from JarvisModule like this:
```c#
using JarvisModuleCore.Classes;
using JarvisModuleCore.ML;
…
[JarvisModule]
public class ExampleModule : JarvisModule
{
  …
}
```
If your module depends on any other libraries (dll files that are copied into the execution directory), add them to the lib folder as mentioned above and specify their file names in the JarvisModuleAttribute like this:
```c#
[JarvisModule(new string[] { "ExampleDependency.dll" })]
public class ExampleModule : JarvisModule
…
```
Any task that JARVIS should be able to execute must be public, non-static and in a module class, annotated and typed like this (the async keyword can be left out, if you want):
```c#
[JarvisTask("example.taskid")]
public async void ExampleTask(Telegram.Bot.Types.Message message, Jarvis jarvis)
{
  …
}
```
For your module class to compile, you will have to override several things:
- Id: a unique identifier string for your module, recommended to be used as a prefix for your task ids
- Name: a name for your module
- Version: a version number for your module. If you don't know what to use, go with ```Version.Parse("1.0.0")```
- MLTrainingData: an array of TaskPredicitionInputs to train the model to recognize your tasks. I recommend about 50 elements per task. An example data set can be found [here](JarvisButlerBot/Training/Ping.json). For the prediction, the bots individual username will be replaced by `@Username`, usernames of other users by `@User` and inline mentions by `@Mention`.

## Publishing your modules
If you wrote your own module and want it to be added to @JarvisButlerBot on Telegram, message me at [@Olfi01](http://t.me/Olfi01), I will happily add it if I think it fits!
