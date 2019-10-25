using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchi {

    public class GotchiStatusLuaScript {

        // Public members

        public const string OnRegisterName = "OnRegister";
        public const string OnAcquireName = "OnAcquire";
        public const string OnClearName = "OnClear";
        public const string OnTurnEnd = "OnTurnEnd";

        public GotchiStatusLuaScript(string scriptFilePath) {

            _script = LuaUtils.CreateAndInitializeScript();

            if (System.IO.File.Exists(scriptFilePath))
                _script.DoFile(scriptFilePath);

        }

        public async Task<bool> OnRegisterAsync(GotchiStatus move) {

            if (_script.Globals[OnRegisterName] != null) {

                await _script.CallAsync(_script.Globals[OnRegisterName], move);

                return true;

            }

            return false;

        }
        public async Task<bool> OnAcquireAsync(GotchiMoveCallbackArgs args) {

            if (_script.Globals[OnAcquireName] != null) {

                await _script.CallAsync(_script.Globals[OnAcquireName], args);

                return true;

            }

            return false;


        }
        public async Task<bool> OnClearAsync(GotchiMoveCallbackArgs args) {

            if (_script.Globals[OnClearName] != null) {

                await _script.CallAsync(_script.Globals[OnClearName], args);

                return true;

            }

            return false;

        }
        public async Task<bool> OnTurnEndAsync(GotchiMoveCallbackArgs args) {

            if (_script.Globals[OnTurnEnd] != null) {

                await _script.CallAsync(_script.Globals[OnTurnEnd], args);

                return true;

            }

            return false;

        }

        // Private members

        private Script _script;

    }

}