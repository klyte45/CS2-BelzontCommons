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

---

## Localization (i18n)

Localization strings are stored in `i18n/i18n.csv` relative to project root as a tab-separated file (`key\ten-US\tpt-BR\t...`). The `en-US` column is the only mandatory one. Additional language columns use the game's locale tag (e.g., `zh-HANS`, `de-DE`).

The Excel file `i18n/i18n.xlsm` has macros that generates automatically the `i18n.csv` file when you press Ctrl+B (for PT-BR Excel users, that's the shortcut for save the file - it works even if your Excel is in another language but will require manual save after editing the translations). Enable macros and edit the `i18n.xlsm` file to add/remove languages or change the formatting as needed. You can find it in other projects from Klyte's GitHub repositories, like [CS2-WriteEverywhere](https://github.com/klyte45/CS2-WriteEverywhere).

`BasicIMod.ProcessKey(key, locale)` is used at load time to translate special `::` prefixed keys into the CS2 Settings UI locale IDs. These are listed below.

### Special Key Prefixes

All of the following prefixes are resolved relative to the **current mod** (i.e., the modData instance registered in `BasicIMod`).

| Key pattern | Resolved to | CS2 UI concept |
|---|---|---|
| `::M` | `modData.GetBindingMapLocaleID()` | The keybinding section name shown in the mod list at Options menu |
| `::G{groupName}` | `modData.GetOptionGroupLocaleID(groupName)` | Section/group header inside the mod's Settings page |
| `::T{tabName}` | `modData.GetOptionTabLocaleID(tabName)` | Tab label inside the mod's Settings page |
| `::L{propName}` | `modData.GetOptionLabelLocaleID(propName)` | Label for a settings option (property name as declared in `BasicModData`) |
| `::D{propName}` | `modData.GetOptionDescLocaleID(propName)` | Description tooltip for a settings option |
| `::B{actionName}` | `modData.GetBindingKeyLocaleID(actionName)` | The display label for a keybinding shown in the mod list at Options menu |
| `::H{actionName}` | `modData.GetBindingKeyHintLocaleID(actionName)` | The hint text shown in the HUD while the keybinding is active |
| `::E{TypeName}.{valueName}` | `modData.GetEnumValueLocaleID(TypeName, valueName)` | Display string for an enum value in a settings dropdown |
| `::NT{notifId}` | `Menu.NOTIFICATION_TITLE[notifId]` | Notification popup title |
| `::ND{notifId}` | `Menu.NOTIFICATION_DESCRIPTION[notifId]` | Notification popup description body |

Keys that do **not** start with `::` are used verbatim and are typically referenced by the frontend (TypeScript/React) via `engine.translate(key)` or directly by C# code via `GameManager.instance.localizationManager.activeDictionary.TryGetValue(key, out string value)`.

### Example (i18n.csv excerpt)

```
key	en-US	pt-BR
::M	My Mod Name	My Mod Name
::GMain	Main	Principal
::TGeneral	General	Geral
::LMyProperty	My Setting Label	Rótulo da Configuração
::DMyProperty	Description for My Setting.	Descrição da configuração.
::BMyMod.MyAction	Toggle My Feature	Ativar/Desativar Funcionalidade
::HMyMod.MyAction	Toggles My Feature on/off (default: Ctrl+Shift+M).	Ativa/desativa a funcionalidade (padrão: Ctrl+Shift+M).
::EMyEnum.ValueA	Value A	Valor A
MyMod.frontend.someLabel	Some Label	Algum Rótulo
```

### Matching `{groupName}` / `{tabName}` / `{propName}` to C# declarations

- **`{tabName}`** must match the **first** string argument of `[SettingsUISection(tab, group)]` on each property. A `::T{tab}` key **must** exist in i18n.csv or the tab label will be blank in the game UI.
- **`{groupName}`** must match the **second** string argument of `[SettingsUISection(tab, group)]`. Corresponds to `[SettingsUIShowGroupName]` on the class. A `::G{group}` key **must** exist in i18n.csv.
- Every settings property on a `BasicModData` subclass **must** carry `[SettingsUISection(tab, group)]`; omitting it causes the property to appear in a blank, untitled group.
- **`{propName}`** must match the **C# property name** (not a custom string) on the `BasicModData` subclass.
- **`{actionName}`** must match the action name string used when registering the keybinding (e.g., `"MyMod.MyAction"`).
- **`{TypeName}.{valueName}`** must match the **C# type name** (not the fully-qualified namespace) and the **enum field name**.
