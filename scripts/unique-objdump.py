#!/usr/bin/python
# compress functions into sentences for bivec
import sys, getopt
import subprocess

def findInstruction(line):
    instruction = ""
    parts = line.split()
    parts = parts[1:]
    for part in parts:
        try:
            int(part, 16)
            if(part == 'add' or part == 'b' or part == 'fadd'):
                instruction = part.upper() + " "
                break
        except:
            instruction = part.upper() + " "
            break

    return instruction

def main(argv):
    elfFile = ""
    try:
        opts, args = getopt.getopt(argv,"hve:")
    except getopt.GetoptError:
        print sys.argv[0] + " -h, -v, -e"
        sys.exit(2)
    for opt, arg in opts:
        if opt == '-h':
            print sys.argv[0] + " -h, -v, -e"
            sys.exit()
        elif opt == '-v':
            print '0.0.1'
            sys.exit()
        elif opt == '-e':
            elfFile = arg

    output = subprocess.check_output(['llvm-objdump', '--x86-asm-syntax', 'intel', '-d', elfFile])
    lines = output.split('\n')

    addresses = []
    for line in lines:
        if(line.startswith(' ') and (line.find('Disassembly of') == -1) and line != ''):
            addresses.append(line)

    instructions = []
    for address in addresses:
        instructions.append(findInstruction(address))
    
    unique = []
    for instruction in instructions:
        if instruction not in unique:
            unique.append(instruction)

    for mnem in unique:
        print mnem

if __name__ == "__main__":
    main(sys.argv[1:])
