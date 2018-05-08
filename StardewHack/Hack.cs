using Harmony;
using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace StardewHack
{
    internal delegate IEnumerable<CodeInstruction> TranspilerSignature(ILGenerator generator, IEnumerable<CodeInstruction> instructions);

    public abstract class Hack : Mod
    {
        /** The harmony instance used for patching. */
        public HarmonyInstance harmony { get; private set; }

        /** The code that is being patched. 
         * Use only within methods annotated with BytecodePatch. 
         */
        public List<CodeInstruction> codes { get; private set; }

        /** The generator used for patching. 
         * Use only within methods annotated with BytecodePatch. 
         */
        public ILGenerator generator { get; private set; }

        /** Provides simpliied API's for writing mods. */
        public IModHelper helper { get; private set; }

        #pragma warning disable 414
        /** Reference used by dynamic proxy static methods. */
        private static Hack instance;
        private MethodInfo method;
        private MethodInfo patch;
        #pragma warning restore 414

        /** Applies the methods annotated with BytecodePatch defined in this class. */
        public override void Entry(IModHelper helper) {
            this.helper = helper;
            Hack.instance = this;

            // Use the Mod's UniqueID to create the harmony instance.
            string UniqueID = helper.ModRegistry.ModID;
            Monitor.Log($"Applying bytecode patches for {UniqueID}.", LogLevel.Info);
            harmony = HarmonyInstance.Create(UniqueID);

            // Iterate all methods in this class and search for those that have a BytecodePatch annotation.
            var methods = this.GetType().GetMethods(AccessTools.all);
            foreach (MethodInfo patch in methods) {
                var bytecode_patches = patch.GetCustomAttributes<BytecodePatch>();
                foreach (var bp in bytecode_patches) {
                    // Apply the patch to the method specified in the annotation.
                    ChainPatch(bp.GetMethod(), patch);
                }
            }
        }

        /** Applies the given patch to the given method. 
         * This method can be called from within a patch method, for example to patch delegate functions. */
        public void ChainPatch(MethodInfo method, MethodInfo patch) {
            var old_generator = this.generator;
            var old_codes = this.codes;

            this.method = method;
            this.patch = patch;

            var apply = AccessTools.Method(typeof(Hack), "ApplyPatch");
            harmony.Patch(method, null, null, new HarmonyMethod(apply));

            this.generator = old_generator;
            this.codes = old_codes;
        }

        /** Called from dynamic proxy method to prepare for patching. */ 
        private static IEnumerable<CodeInstruction> ApplyPatch(ILGenerator generator, IEnumerable<CodeInstruction> instructions) {
            string info = $"Applying patch {instance.patch.Name} to {instance.method} in {instance.method.DeclaringType.FullName}.";
            instance.generator = generator;
            instance.codes = new List<CodeInstruction>(instructions);
            instance.Monitor.Log(info);
            instance.patch.Invoke(instance, null);
            return instance.codes;
        }

        /** Find the first occurance of the given sequence of instructions that follows this range.
         * The performed matching depends on the type:
         *  - String: is it contained in the string representation of the instruction
         *  - MemberReference (including MethodDefinition): is the instruction's operand equal to this reference.
         *  - OpCode: is this the instruction's OpCode.
         *  - CodeInstruction: are the instruction's OpCode and Operand equal.
         *  - null: always matches.
         */
        public InstructionRange FindCode(params Object[] contains) {
            return new InstructionRange(codes, contains);
        }

        /** Find the last occurance of the given sequence of instructions that follows this range.
         * See FindCode() for how the matching is performed.
         */
        public InstructionRange FindCodeLast(params Object[] contains) {
            return new InstructionRange(codes, contains, codes.Count, -1);
        }

        public InstructionRange BeginCode() {
            return new InstructionRange(codes, 0, 0);
        }

        public InstructionRange EndCode() {
            return new InstructionRange(codes, codes.Count, 0);
        }

        public static void Log(string message, LogLevel level=LogLevel.Debug) {
            instance.Monitor.Log(message, level);
        }

        public static Label attachLabel(CodeInstruction target) {
            var lbl = instance.generator.DefineLabel();
            target.labels.Add(lbl);
            return lbl;
        }
    }
}

