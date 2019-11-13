using MoonSharp.Interpreter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OurFoodChain.Gotchis {

    public class GotchiMoveLuaScript {

        // Public members

        public const string OnRegisterName = "OnRegister";
        public const string OnInitName = "OnInit";
        public const string OnMoveName = "OnMove";

        public GotchiMoveLuaScript(string scriptFilePath) {

            _script = LuaUtils.CreateAndInitializeScript();

            if (System.IO.File.Exists(scriptFilePath))
                _script.DoFile(scriptFilePath);

        }

        public async Task<bool> OnRegisterAsync(GotchiMove move) {

            if (_script.Globals[OnRegisterName] != null) {

                await _script.CallAsync(_script.Globals[OnRegisterName], move);

                return true;

            }

            return false;

        }
        public async Task<bool> OnInitAsync(GotchiMoveCallbackArgs args) {

            if (_script.Globals[OnInitName] != null) {

                await _script.CallAsync(_script.Globals[OnInitName], args);

                return true;

            }

            return false;


        }
        public async Task<bool> OnMoveAsync(GotchiMoveCallbackArgs args) {

            if (_script.Globals[OnMoveName] != null) {

                await _script.CallAsync(_script.Globals[OnMoveName], args);

                return true;

            }

            return false;

        }

        // Private members

        private Script _script;

    }

}