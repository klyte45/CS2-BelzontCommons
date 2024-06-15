# Belzont Commons - C# Library for Cities Skylines 2

This library offers some utility methods that I use in Cities Skylines 2 development. It's an evolution from Klyte Commons and Kwytto Utility libraries that I used in Cities Skylines 1.

Some highlights are:
- `Frontend.targets` contains automation to compile multiple frontend subprojects, both using EUIS or Vanilla APIs
- `IBelzontSerializableSingleton` contains utility to save/load system data. It uses default jobs to make the work of serialization/deserialization of system files.
- `IBelzontBindable` is meant to be used along EUIS to register event/call/trigger bindings throught all UI screens enabled and also the vanilla UI as well. More details at EUIS documentation.
- `BasicIMod` contains a lot of common tasks when developing a CS2 mod, like loading locales, registering bindings, etc. It's an evolution from the CS1 version.
- `BasicIModData` contains basic data for the mod options at settings screen. It shows debug settings and also the mod version.
- `LogUtils` contains utility for doing logging stuff. **Don't forget to rename the log file prefix if plan to use this library**

I recommend to not use this library directly, but creating your own fork instead. I may change a lot of stuff to fit my needs and it might break someone else mod...
I also recommend to use this library as a folder inside the main dll of your mod, or merging all DLLs after compiling the solution like I did on [CS2 BuildFiles target file](https://github.com/klyte45/CS2-BuildFiles/blob/master/belzont_public.targets).
