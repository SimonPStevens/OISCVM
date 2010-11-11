﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OISC_Compiler.Instructions;

namespace OISC_Compiler
{
    public interface IAssembler
    {
        byte[] Assemble();
    }

    public class OISCAsm :IAssembler
    {
        private String[] _sourceCodeLines;

        public OISCAsm(String[] sourceCodeLines)
        {
            _sourceCodeLines = sourceCodeLines;
        }

        public byte[] Assemble()
        {
            ICollection<ExecutableInstruction> sourceTree = ParseSource();
            
            // Resolve the binary addresses for each instruction.
            Int64 currentBinaryAddress = 0;
            foreach (ExecutableInstruction instruction in sourceTree)
            {
                instruction.SetBinaryAddress(currentBinaryAddress);
                currentBinaryAddress += instruction.BinaryAddressLength;
            }

            // Create an array of the required size to hold the binary data.
            // Size is taken from the final binary address used.
            byte[] binary = new byte[currentBinaryAddress];

            // Assemble the binary for each instruction and store it in the array.
            foreach (ExecutableInstruction instruction in sourceTree)
            {
                byte[] instructionBinary = instruction.AssembleBinary();
                Array.Copy(instructionBinary, 0, binary, instruction.BinaryAddress, instruction.BinaryAddressLength);
            }

            return binary;
        }

        private ICollection<ExecutableInstruction> ParseSource()
        {
            InstructionFactory instructionParser = new InstructionFactory();

            // We parse the source and build a list of all source 
            // instructions (including comments), and a dictionary
            // of executable instructions, indexed by their source
            // starting address.
            List<Instruction> sourceList = new List<Instruction>();
            Dictionary<int, ExecutableInstruction> instructionDictionary = new Dictionary<int, ExecutableInstruction>();
            int instructionSourceAddress = 0;
            int instructionSourceLineNumber = 0;

            // Loop through each line of source code and create an instruction for it.
            foreach (String sourceLine in _sourceCodeLines)
            {
                Instruction sourceInstruction = instructionParser.GenerateInstruction(sourceLine, instructionSourceLineNumber, instructionSourceAddress);

                ExecutableInstruction executableInstruction = sourceInstruction as ExecutableInstruction;
                if (executableInstruction != null)
                {
                    instructionDictionary.Add(instructionSourceAddress, executableInstruction);
                    instructionSourceAddress += executableInstruction.SourceAddressLength;
                }
                sourceList.Add(sourceInstruction);

                instructionSourceLineNumber++;
            }

            // Loop through each instruction and map each branch address to the destination instruction.
            // This is done at the source level at this stage so we have a correctly mapped instruction 
            // tree before we start generating binary and create the actual addresses.
            foreach (var instruction in instructionDictionary)
            {
                IBranchingInstruction branchingInstruction = instruction.Value as IBranchingInstruction;
                if (branchingInstruction != null)
                {
                    branchingInstruction.MapBranchAddress(instructionDictionary);
                }
            }

            return instructionDictionary.Values;
        }


    }
}
