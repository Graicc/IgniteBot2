﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace IgniteBot.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.8.1.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool autoRestart {
            get {
                return ((bool)(this["autoRestart"]));
            }
            set {
                this["autoRestart"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool showDatabaseLog {
            get {
                return ((bool)(this["showDatabaseLog"]));
            }
            set {
                this["showDatabaseLog"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool logToServer {
            get {
                return ((bool)(this["logToServer"]));
            }
            set {
                this["logToServer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool startOnBoot {
            get {
                return ((bool)(this["startOnBoot"]));
            }
            set {
                this["startOnBoot"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int targetDeltaTimeIndexStats {
            get {
                return ((int)(this["targetDeltaTimeIndexStats"]));
            }
            set {
                this["targetDeltaTimeIndexStats"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool useCompression {
            get {
                return ((bool)(this["useCompression"]));
            }
            set {
                this["useCompression"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool batchWrites {
            get {
                return ((bool)(this["batchWrites"]));
            }
            set {
                this["batchWrites"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("none")]
        public string saveFolder {
            get {
                return ((string)(this["saveFolder"]));
            }
            set {
                this["saveFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool enableFullLogging {
            get {
                return ((bool)(this["enableFullLogging"]));
            }
            set {
                this["enableFullLogging"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool enableStatsLogging {
            get {
                return ((bool)(this["enableStatsLogging"]));
            }
            set {
                this["enableStatsLogging"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int targetDeltaTimeIndexFull {
            get {
                return ((int)(this["targetDeltaTimeIndexFull"]));
            }
            set {
                this["targetDeltaTimeIndexFull"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool showConsoleOnStart {
            get {
                return ((bool)(this["showConsoleOnStart"]));
            }
            set {
                this["showConsoleOnStart"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool outputGameStateEvents {
            get {
                return ((bool)(this["outputGameStateEvents"]));
            }
            set {
                this["outputGameStateEvents"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool outputScoreEvents {
            get {
                return ((bool)(this["outputScoreEvents"]));
            }
            set {
                this["outputScoreEvents"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool outputStunEvents {
            get {
                return ((bool)(this["outputStunEvents"]));
            }
            set {
                this["outputStunEvents"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool outputDiscThrownEvents {
            get {
                return ((bool)(this["outputDiscThrownEvents"]));
            }
            set {
                this["outputDiscThrownEvents"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool outputDiscCaughtEvents {
            get {
                return ((bool)(this["outputDiscCaughtEvents"]));
            }
            set {
                this["outputDiscCaughtEvents"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool outputDiscStolenEvents {
            get {
                return ((bool)(this["outputDiscStolenEvents"]));
            }
            set {
                this["outputDiscStolenEvents"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool outputSaveEvents {
            get {
                return ((bool)(this["outputSaveEvents"]));
            }
            set {
                this["outputSaveEvents"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string echoVRPath {
            get {
                return ((string)(this["echoVRPath"]));
            }
            set {
                this["echoVRPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string accessCode {
            get {
                return ((string)(this["accessCode"]));
            }
            set {
                this["accessCode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool outputOther {
            get {
                return ((bool)(this["outputOther"]));
            }
            set {
                this["outputOther"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool uploadToIgniteDB {
            get {
                return ((bool)(this["uploadToIgniteDB"]));
            }
            set {
                this["uploadToIgniteDB"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool uploadToFirestore {
            get {
                return ((bool)(this["uploadToFirestore"]));
            }
            set {
                this["uploadToFirestore"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool startMinimized {
            get {
                return ((bool)(this["startMinimized"]));
            }
            set {
                this["startMinimized"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool atlasShowing {
            get {
                return ((bool)(this["atlasShowing"]));
            }
            set {
                this["atlasShowing"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UpdateSettings {
            get {
                return ((bool)(this["UpdateSettings"]));
            }
            set {
                this["UpdateSettings"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool onlyRecordPrivateMatches {
            get {
                return ((bool)(this["onlyRecordPrivateMatches"]));
            }
            set {
                this["onlyRecordPrivateMatches"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool speedometerStreamerMode {
            get {
                return ((bool)(this["speedometerStreamerMode"]));
            }
            set {
                this["speedometerStreamerMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int whenToSplitReplays {
            get {
                return ((int)(this["whenToSplitReplays"]));
            }
            set {
                this["whenToSplitReplays"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("127.0.0.1")]
        public string echoVRIP {
            get {
                return ((string)(this["echoVRIP"]));
            }
            set {
                this["echoVRIP"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool playspaceStreamerMode {
            get {
                return ((bool)(this["playspaceStreamerMode"]));
            }
            set {
                this["playspaceStreamerMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("6721")]
        public int echoVRPort {
            get {
                return ((int)(this["echoVRPort"]));
            }
            set {
                this["echoVRPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool joustTimeTTS {
            get {
                return ((bool)(this["joustTimeTTS"]));
            }
            set {
                this["joustTimeTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool joustSpeedTTS {
            get {
                return ((bool)(this["joustSpeedTTS"]));
            }
            set {
                this["joustSpeedTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool serverLocationTTS {
            get {
                return ((bool)(this["serverLocationTTS"]));
            }
            set {
                this["serverLocationTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool maxBoostSpeedTTS {
            get {
                return ((bool)(this["maxBoostSpeedTTS"]));
            }
            set {
                this["maxBoostSpeedTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1")]
        public int TTSSpeed {
            get {
                return ((int)(this["TTSSpeed"]));
            }
            set {
                this["TTSSpeed"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool playerJoinTTS {
            get {
                return ((bool)(this["playerJoinTTS"]));
            }
            set {
                this["playerJoinTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool playerLeaveTTS {
            get {
                return ((bool)(this["playerLeaveTTS"]));
            }
            set {
                this["playerLeaveTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool playerSwitchTeamTTS {
            get {
                return ((bool)(this["playerSwitchTeamTTS"]));
            }
            set {
                this["playerSwitchTeamTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool tubeExitSpeedTTS {
            get {
                return ((bool)(this["tubeExitSpeedTTS"]));
            }
            set {
                this["tubeExitSpeedTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool discordRichPresence {
            get {
                return ((bool)(this["discordRichPresence"]));
            }
            set {
                this["discordRichPresence"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string discordOAuthRefreshToken {
            get {
                return ((string)(this["discordOAuthRefreshToken"]));
            }
            set {
                this["discordOAuthRefreshToken"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool throwSpeedTTS {
            get {
                return ((bool)(this["throwSpeedTTS"]));
            }
            set {
                this["throwSpeedTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool goalSpeedTTS {
            get {
                return ((bool)(this["goalSpeedTTS"]));
            }
            set {
                this["goalSpeedTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool goalDistanceTTS {
            get {
                return ((bool)(this["goalDistanceTTS"]));
            }
            set {
                this["goalDistanceTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string accessMode {
            get {
                return ((string)(this["accessMode"]));
            }
            set {
                this["accessMode"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int clientHighlightScope {
            get {
                return ((int)(this["clientHighlightScope"]));
            }
            set {
                this["clientHighlightScope"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool clearHighlightsOnExit {
            get {
                return ((bool)(this["clearHighlightsOnExit"]));
            }
            set {
                this["clearHighlightsOnExit"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool isAutofocusEnabled {
            get {
                return ((bool)(this["isAutofocusEnabled"]));
            }
            set {
                this["isAutofocusEnabled"] = value;
            }
        }

        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool isNVHighlightsEnabled {
            get {
                return ((bool)(this["isNVHighlightsEnabled"]));
            }
            set {
                this["isNVHighlightsEnabled"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3")]
        public float nvHighlightsSecondsBefore {
            get {
                return ((float)(this["nvHighlightsSecondsBefore"]));
            }
            set {
                this["nvHighlightsSecondsBefore"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("2")]
        public float nvHighlightsSecondsAfter {
            get {
                return ((float)(this["nvHighlightsSecondsAfter"]));
            }
            set {
                this["nvHighlightsSecondsAfter"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool pausedTTS {
            get {
                return ((bool)(this["pausedTTS"]));
            }
            set {
                this["pausedTTS"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("127.0.0.1")]
        public string alternateEchoVRIP {
            get {
                return ((string)(this["alternateEchoVRIP"]));
            }
            set {
                this["alternateEchoVRIP"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool capturevp2 {
            get {
                return ((bool)(this["capturevp2"]));
            }
            set {
                this["capturevp2"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool nvHighlightsSpectatorRecord {
            get {
                return ((bool)(this["nvHighlightsSpectatorRecord"]));
            }
            set {
                this["nvHighlightsSpectatorRecord"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int atlasLinkStyle {
            get {
                return ((int)(this["atlasLinkStyle"]));
            }
            set {
                this["atlasLinkStyle"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool atlasLinkUseAngleBrackets {
            get {
                return ((bool)(this["atlasLinkUseAngleBrackets"]));
            }
            set {
                this["atlasLinkUseAngleBrackets"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool firstTimeSetupShown {
            get {
                return ((bool)(this["firstTimeSetupShown"]));
            }
            set {
                this["firstTimeSetupShown"] = value;
            }
        }
    }
}
