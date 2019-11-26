using System;
using System.Reflection.Emit;
using Harmony;

namespace StardewHack
{
    abstract public class InstructionMatcher
    {
        public abstract bool match(CodeInstruction instruction);
        
        public static implicit operator InstructionMatcher(CodeInstruction query) => new IM_CodeInstruction(query);
        public static implicit operator InstructionMatcher(OpCode query) => new IM_OpCode(query);
    }
    
    internal class IM_CodeInstruction : InstructionMatcher
    {
        readonly CodeInstruction query;
        public IM_CodeInstruction(CodeInstruction q) {
            query = q;
        }

        public override bool match(CodeInstruction instruction) {
            // Exact match.
            if (query == instruction) return true;
            
            // Check Opcode
            if (!instruction.opcode.Equals(query.opcode)) return false;
            
            // Check Operand
            if (instruction.operand == null) {
                if (query.operand != null) return false;
            } else if (!instruction.operand.Equals(query.operand)) {
                if (query.operand==null) return false;

                // In case the operand is an integer, but their boxing types don't match.
                try {
                    if (Convert.ToInt64(instruction.operand) != Convert.ToInt64(query.operand)) return false;
                } catch {
                    return false;
                }
            }
            return true;
        }
    }
    
    internal class IM_OpCode : InstructionMatcher
    {
        readonly OpCode query;
        public IM_OpCode(OpCode q) {
            query = q;
        }

        public override bool match(CodeInstruction instruction) {
            return instruction.opcode.Equals(query);
        }
    }
}
