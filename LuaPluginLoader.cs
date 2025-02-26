using Chorizite.Core.Plugins;
using Chorizite.Core.Plugins.AssemblyLoader;
using Microsoft.Extensions.Logging;
using System;

namespace Lua {
    internal class LuaPluginLoader : IPluginLoader {
        private readonly LuaPluginCore _lua;

        public LuaPluginLoader(LuaPluginCore lua) {
            _lua = lua;
        }

        public bool CanLoadPlugin(PluginManifest manifest) {
            return manifest.EntryFile?.EndsWith(".lua") == true;
        }

        public bool LoadPluginInstance(PluginManifest manifest, out PluginInstance? instance) {
            if (PluginManifest.TryLoadManifest<LuaPluginManifest>(manifest.ManifestFile, out var assemblyManifest, out string? errors)) {
                instance = new LuaPluginInstance(assemblyManifest, _lua.Scope);
                return true;
            }
            LuaPluginCore.Log?.LogError(errors);
            instance = null;
            return false;
        }
    }
}