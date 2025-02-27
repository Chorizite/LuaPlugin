using Autofac;
using Chorizite.Core.Plugins;
using Chorizite.Core.Plugins.AssemblyLoader;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Lua {
    /// <inheritdoc />
    public class LuaPluginInstance : PluginInstance<LuaPluginManifest> {
        private LuaPluginManifest luaManifest;
        private object[] _moduleRet;

        public override object? Instance => (_moduleRet is null || _moduleRet.Length == 0) ? null : _moduleRet[0];

        /// <inheritdoc />
        public LuaPluginInstance(LuaPluginManifest assemblyManifest, ILifetimeScope serviceProvider) : base(assemblyManifest, serviceProvider) {
            luaManifest = assemblyManifest;
        }

        /// <inheritdoc />
        public override void Initialize() {
            base.Initialize();
        }

        /// <inheritdoc />
        public override bool Load() {
            var luaEntry = Path.Combine(luaManifest.BaseDirectory, luaManifest.EntryFile);
            if (!File.Exists(luaEntry)) {
                LuaPluginCore.Log.LogError($"Could not find lua entry file: {luaEntry}");
                IsLoaded = false;
                return false;
            }

            var source = File.ReadAllText(luaEntry);
            _moduleRet = LuaPluginCore.Instance.Context.DoString($"""coroutine.create_managed(function() {source} end, "Document")""", $"{luaEntry}");

            IsLoaded = true;
            return base.Load();
        }

        /// <inheritdoc />
        public override bool Unload(bool isReloading) {
            _moduleRet = null;
            return base.Unload(isReloading);
        }

        /// <inheritdoc />
        public override void Dispose() {
            _moduleRet = null;
            base.Dispose();
        }
    }
}