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
            if(part == 'add'):
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
        if(line.startswith(' ') or (line.find('Disassembly of') != -1) or line == ''):
            addresses.append(line)

    functions = [x for x in lines if x not in addresses]
    functions = functions[1:]

    i = 0
    j = 0
    sentence = ""
    for function in functions:
        while(lines[j] != function):
            j+=1
        if(i < len(functions)-1):
            sentence += function + " "
            j+=1
            while(lines[j] != functions[i+1]):
                if(lines[j].find('Disassembly of') != -1 or lines[j] == ''):
                    j+=1
                    continue
                parts = lines[j].split()
                parts = parts[1:]
                for part in parts:
                    try:
                        int(part, 16)
                        if(part == 'add' or part == 'b'):
                            break
                    except:
                        break
                sentence += part.upper() + " "
                j+=1
        else:
            sentence += function + " "
            while(j < len(lines)-1):
                if(lines[j].find('Disassembly of') != -1 or lines[j] == ''):
                    j+=1
                    continue
                sentence += findInstruction(lines[j])
                j+=1
        print sentence
        sentence = ""
        i+=1

if __name__ == "__main__":
    main(sys.argv[1:])