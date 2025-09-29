using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace HiddenHorizons
{
    /// <summary>
    /// Helper class to debug save/load issues
    /// </summary>
    public static class SaveDebugHelper
    {
        public static async Task DebugSaveFile(string saveDirectory, int slotNumber)
        {
            string slotDirectoryName = slotNumber == -1 ? "autosave" : $"slot_{slotNumber}";
            var saveFilePath = Path.Combine(saveDirectory, slotDirectoryName, "save.json");
            
            if (!File.Exists(saveFilePath))
            {
                Console.WriteLine($"❌ Save file not found: {saveFilePath}");
                return;
            }

            try
            {
                var jsonContent = await File.ReadAllTextAsync(saveFilePath);
                var saveData = JsonSerializer.Deserialize<GameSaveData>(jsonContent, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IncludeFields = true
                });

                Console.WriteLine("=== SAVE FILE DEBUG INFO ===");
                Console.WriteLine($"📁 File: {saveFilePath}");
                Console.WriteLine($"📅 Timestamp: {saveData.SaveTimestamp}");
                Console.WriteLine($"🔢 Version: {saveData.Version}");
                Console.WriteLine($"🎮 Game Version: {saveData.GameVersion}");
                Console.WriteLine($"📊 Systems Count: {saveData.SystemData?.Count ?? 0}");
                
                if (saveData.SystemData != null)
                {
                    Console.WriteLine("\n📋 Systems in save file:");
                    foreach (var kvp in saveData.SystemData)
                    {
                        Console.WriteLine($"  • {kvp.Key}");
                        
                        // Special debug for key systems
                        if (kvp.Key == "Adventurer" && kvp.Value is JsonElement advElement)
                        {
                            try
                            {
                                var advData = JsonSerializer.Deserialize<AdventurerSaveData>(advElement.GetRawText());
                                Console.WriteLine($"    Position: {advData.Position}");
                                Console.WriteLine($"    Speed: {advData.Speed}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ❌ Failed to parse Adventurer data: {ex.Message}");
                            }
                        }
                        
                        if (kvp.Key == "TimeManager" && kvp.Value is JsonElement timeElement)
                        {
                            try
                            {
                                var timeData = JsonSerializer.Deserialize<TimeManagerSaveData>(timeElement.GetRawText());
                                Console.WriteLine($"    Current Day: {timeData.CurrentDay}");
                                Console.WriteLine($"    Current Time: {timeData.CurrentTime}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ❌ Failed to parse TimeManager data: {ex.Message}");
                            }
                        }
                        
                        if (kvp.Key == "ZoneManager" && kvp.Value is JsonElement zoneElement)
                        {
                            try
                            {
                                var zoneData = JsonSerializer.Deserialize<Dictionary<string, ZoneSaveData>>(zoneElement.GetRawText());
                                Console.WriteLine($"    Zones Count: {zoneData?.Count ?? 0}");
                                if (zoneData != null)
                                {
                                    foreach (var zone in zoneData.Keys)
                                    {
                                        Console.WriteLine($"      - {zone}");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"    ❌ Failed to parse ZoneManager data: {ex.Message}");
                            }
                        }
                    }
                }
                
                Console.WriteLine("=== END DEBUG INFO ===\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error reading save file: {ex.Message}");
            }
        }

        public static void DebugRegisteredSystems(SaveManager saveManager)
        {
            Console.WriteLine("=== REGISTERED SYSTEMS DEBUG ===");
            Console.WriteLine($"📊 Total registered systems: {saveManager.RegisteredSystemCount}");
            Console.WriteLine("=== END SYSTEMS DEBUG ===\n");
        }
    }
}