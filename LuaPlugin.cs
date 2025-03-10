﻿using System.IO;
using System;
using Microsoft.Extensions.Logging;
using Chorizite.Core.Plugins;
using Chorizite.Core.Plugins.AssemblyLoader;
using Chorizite.Core;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO.Compression;
using Autofac;
using System.Collections.Generic;
using Chorizite.Core.Backend.Client;
using Chorizite.Core.Backend;
using Chorizite.Core.Backend.Launcher;
using Chorizite.Core.Net;
using Chorizite.Core.Render;
using Chorizite.Core.Input;
using Chorizite.Core.Dats;

namespace Lua {
    public class LuaPluginCore : IPluginCore {
        internal static ILogger Log;
        internal readonly ILifetimeScope Scope;
        internal readonly IChoriziteBackend Backend;
        internal readonly Dictionary<string, object> LuaModules = [];
        internal readonly Dictionary<string, Func<object>> LuaModuleCallbacks = [];
        internal static LuaPluginCore Instance;
        private LuaPluginLoader _luaLoader;

        public LuaContext Context { get; private set; }

        protected LuaPluginCore(AssemblyPluginManifest manifest, IChoriziteBackend backend, ILifetimeScope scope, ILogger log) : base(manifest) {
            Instance = this;
            Log = log;
            Scope = scope;
            Backend = backend;
        }

        protected override void Initialize() {
            Context = new LuaContext(this, Log);

            RegisterLuaModule("Backend", Backend);
            RegisterLuaModule("Renderer", Scope.Resolve<IRenderInterface>());
            RegisterLuaModule("InputManager", Scope.Resolve<IInputManager>());
            RegisterLuaModule("PluginManager", Scope.Resolve<IPluginManager>());
            
            // optional interfaces
            RegisterOptionalLuaModule<IDatReaderInterface>("DatReader");
            RegisterOptionalLuaModule<NetworkParser>("NetworkParser");
            RegisterOptionalLuaModule<IClientBackend>("ClientBackend");
            RegisterOptionalLuaModule<ILauncherBackend>("LauncherBackend");

            _luaLoader = new LuaPluginLoader(this);
            Scope.Resolve<IPluginManager>().RegisterPluginLoader(_luaLoader);

            Backend.Renderer.OnRender2D += OnRender2D;
        }

        #region Public API
        /// <summary>
        /// Register a lua module
        /// </summary>
        /// <param name="name"></param>
        /// <param name="module"></param>
        public bool RegisterLuaModule(string name, object module) {
            if (LuaModuleCallbacks.ContainsKey(name) || !LuaModules.TryAdd(name, module)) {
                Log.LogWarning($"Failed to register lua module: {name}. Already exists with value: {LuaModules[name]}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Register a lua module
        /// </summary>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public bool RegisterLuaModule(string name, Func<object> callback) {
            if (LuaModules.ContainsKey(name) || !LuaModuleCallbacks.TryAdd(name, callback)) {
                Log.LogWarning($"Failed to register lua module: {name}. Already exists!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Unregister a lua module
        /// </summary>
        /// <param name="name"></param>
        public bool UnregisterLuaModule(string name) {
            if (LuaModules.Remove(name)) return true;
            if (LuaModuleCallbacks.Remove(name)) return true;
            return false;
        }
        #endregion //Public API

        private void RegisterOptionalLuaModule<T>(string name) {
            if (Scope.TryResolve(typeof(T), out var module)) {
                RegisterLuaModule(name, module);
            }
        }

        private void OnRender2D(object? sender, EventArgs e) {
            try {
                Context?.Update();
            }
            catch (Exception ex) {
                Log?.LogError(ex, "Error in OnRender2D");
            }
        }

        protected override void Dispose() {
            Backend.Renderer.OnRender2D -= OnRender2D;
            Scope.Resolve<IPluginManager>().UnregisterPluginLoader(_luaLoader);
            Context?.Dispose();
            Context = null!;
            Instance = null!;
        }
    }
}
