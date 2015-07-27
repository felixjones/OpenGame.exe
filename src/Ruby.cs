﻿using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenGame
{
    class Ruby
    {
        private ScriptRuntime runtime;
        private ScriptEngine engine;
        private ScriptScope scope;

        public Ruby()
        {
            //Setup the script engine runtime
            var setup = new ScriptRuntimeSetup();
            setup.LanguageSetups.Add(
                new LanguageSetup(
                    "IronRuby.Runtime.RubyContext, IronRuby",
                    "IronRuby 1.0",
                    new[] { "IronRuby", "Ruby", "rb" },
                    new[] { ".rb" }));
            setup.DebugMode = true;
            
            //Create the runtime, engine, and scope
            runtime = ScriptRuntime.CreateRemote(AppDomain.CurrentDomain, setup);
            engine = runtime.GetEngine("Ruby");
            scope = engine.CreateScope();

            //Load system internals and our Ruby internals
            Console.WriteLine("Loading system");
            //engine.Execute(System.Text.Encoding.UTF8.GetString(Properties.Resources.System), scope);
            string script = System.Text.Encoding.UTF8.GetString(Properties.Resources.System);
            script = script.Substring(1);  //fix for a weird character that shouldn't be there o.O
            Eval(script);
            Console.WriteLine("Loading loader");
            script = System.Text.Encoding.UTF8.GetString(Properties.Resources.Loader);
            script = script.Substring(1); //fix for weird initial character
            Eval(script);
            Console.WriteLine("Loader loaded");

            //Load the version appropriate RPG datatypes
            //TODO: Add RPG1 datatypes
            if (Program.GetRGSSVersion() == 2)
            {
                script = System.Text.Encoding.UTF8.GetString(Properties.Resources.RPG2);
                script = script.Substring(1);
                Eval(script);
            }
            if (Program.GetRGSSVersion() == 3)
            {
                script = System.Text.Encoding.UTF8.GetString(Properties.Resources.RPG3);
                script = script.Substring(1);
                Eval(script);
            }
        }

        public void Start()
        {
            Graphics.initialize(Program.GetRGSSVersion(), Program.GetRtp().GetPath(), Program.Window, Program.ResolutionWidth, Program.ResolutionHeight);
            engine.Execute(Window.ruby_helper(), scope);
            engine.Execute(CTilemap.ruby_helper(), scope);
            engine.Execute(Viewport.ruby_helper(), scope);
            engine.Execute(Rect.ruby_helper(), scope);
            engine.Execute(Sprite.ruby_helper(), scope);
            engine.Execute(Color.ruby_helper(), scope);
            engine.Execute(Tone.ruby_helper(), scope);
            engine.Execute(Bitmap.ruby_helper(), scope);
            Font.load_fonts();
            engine.Execute(Font.ruby_helper(), scope);
            try
            {
                engine.Execute(@"
                    $RGSS_VERSION = " + Program.GetRGSSVersion() + @"
                    rgss_start
                ", scope);
            }
            catch (Exception e)
            {
                Program.Error(e.Message);
            }
        }

        public void Eval(string str)
        {
            try
            {
                var source = engine.CreateScriptSourceFromString(str);
                source.Compile(new ReportingErrorListener());
                source.Execute(scope);
            }
            catch (Exception e)
            {
                Program.Error(e.Message);
            }
        }

        public void Dispose()
        {
            scope = null;
            engine.Runtime.Shutdown();
            engine = null;
        }
    }

    public class ReportingErrorListener : ErrorListener
    {
        public override void ErrorReported(ScriptSource source, string message, SourceSpan span, int errorCode, Severity severity)
        {
            Program.Error(message);
        }
    }
}
