import subprocess
import sys
import os
import threading
import queue

# Including the KeyValues class from https://github.com/gorgitko/valve-keyvalues-python
# Vendoring this so there's no dependency on an external library & we can keep everything in one file
class KeyValues(dict):
    # The MIT License (MIT)

    # Copyright (c) 2016 Jiri Novotny

    # Permission is hereby granted, free of charge, to any person obtaining a copy
    # of this software and associated documentation files (the "Software"), to deal
    # in the Software without restriction, including without limitation the rights
    # to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    # copies of the Software, and to permit persons to whom the Software is
    # furnished to do so, subject to the following conditions:

    # The above copyright notice and this permission notice shall be included in all
    # copies or substantial portions of the Software.

    # THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    # IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    # FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    # AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    # LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    # OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    # SOFTWARE.
    # __author__ = "Jiri Novotny"
    # __version__ = "1.0.0"
    """
    Class for manipulation with Valve KeyValue (KV) files (VDF format). Parses the KV file to object with dict interface.
    Allows to write objects with dict interface to KV files.
    """

    __re = __import__('re')
    __sys = __import__('sys')
    __OrderedDict = __import__('collections').OrderedDict
    __regexs = {
        "key": __re.compile(r"""(['"])(?P<key>((?!\1).)*)\1(?!.)""", __re.I),
        "key_value": __re.compile(r"""(['"])(?P<key>((?!\1).)*)\1(\s+|)['"](?P<value>((?!\1).)*)\1""", __re.I)
    }
    
    def __init__(self, mapper=None, filename=None, encoding="utf-8", mapper_type=__OrderedDict, key_modifier=None, key_sorter=None):
        """
        :param mapper: initialize with own dict-like mapper
        :param filename: filename of KV file, which will be parsed to dict structure. Mapper param must not be specified when using this param!
        :param encoding: KV file encoding. Default: 'utf-8'
        :param mapper_type: which mapper will be used for storing KV. It must have the dict interface, i.e. allow to do the 'mapper[key] = value action'.
                default: 'collections.OrderedDict'
                For example you can use the 'dict' type.
        :param key_modifier: function for modifying the keys, e.g. the function 'string.lower' will make all the keys lower
        :param key_sorter: function for sorting the keys when dumping/writing/str, e.g. using the function 'sorted' will show KV keys in alphabetical order
        """

        self.__sys.setrecursionlimit(100000)
        self.mapper_type = type(mapper) if mapper else mapper_type
        self.key_modifier = key_modifier
        self.key_sorter = key_sorter

        if not mapper and not filename:
            self.__mapper = mapper_type()
            return

        if mapper:
            self.__mapper = mapper
            return

        if type(filename) == str:
            self.parse(filename)
        else:
            raise Exception("'filename' argument must be string!")

    def __setitem__(self, key, item):
        self.__mapper[key] = item

    def __getitem__(self, key):
        return self.__mapper[key]

    def __repr__(self):
        #return repr(self.__mapper)
        return self.dump(self.__mapper)

    def __len__(self):
        return len(self.__mapper)

    def __delitem__(self, key):
        del self.__mapper[key]

    def clear(self):
        return self.__mapper.clear()

    def copy(self):
        """
        :return: mapper of KeyValues
        """
        return self.__mapper.copy()

    def has_key(self, k):
        return self.__mapper.has_key(k)

    def pop(self, k, d=None):
        return self.__mapper.pop(k, d)

    def update(self, *args, **kwargs):
        return self.__mapper.update(*args, **kwargs)

    def keys(self):
        return self.__mapper.keys()

    def values(self):
        return self.__mapper.values()

    def items(self):
        return self.__mapper.items()

    def pop(self, *args):
        return self.__mapper.pop(*args)

    def __cmp__(self, dict):
        return cmp(self.__mapper, dict)

    def __contains__(self, item):
        return item in self.__mapper

    def __iter__(self):
        return iter(self.__mapper)

    def __unicode__(self):
        return unicode(repr(self.__mapper))

    def __str__(self):
        return self.dump()

    def __key_modifier(self, key, key_modifier):
        """
        Modifies the key string using the 'key_modifier' function.

        :param key:
        :param key_modifier:
        :return:
        """

        key_modifier = key_modifier or self.key_modifier

        if key_modifier:
            return key_modifier(key)
        else:
            return key

    def __parse(self, lines, mapper_type, i=0, key_modifier=None):
        """
        Recursively maps the KeyValues from list of file lines.

        :param lines:
        :param mapper_type:
        :param i:
        :param key_modifier:
        :return:
        """

        key = False
        _mapper = mapper_type()

        try:
            while i < len(lines):
                if lines[i].startswith("{"):
                    if not key:
                        raise Exception("'{{' found without key at line {}".format(i + 1))
                    _mapper[key], i = self.__parse(lines, i=i+1, mapper_type=mapper_type, key_modifier=key_modifier)
                    continue
                elif lines[i].startswith("}"):
                    return _mapper, i + 1
                elif self.__re.match(self.__regexs["key"], lines[i]):
                    key = self.__key_modifier(self.__re.search(self.__regexs["key"], lines[i]).group("key"), key_modifier)
                    i += 1
                    continue
                elif self.__re.match(self.__regexs["key_value"], lines[i]):
                    groups = self.__re.search(self.__regexs["key_value"], lines[i])
                    _mapper[self.__key_modifier(groups.group("key"), key_modifier)] = groups.group("value")
                    i += 1
                elif self.__re.match(self.__regexs["key_value"], lines[i] + lines[i+1]):
                    groups = self.__re.search(self.__regexs["key_value"], lines[i] + " " + lines[i+1])
                    _mapper[self.__key_modifier(groups.group("key"), key_modifier)] = groups.group("value")
                    i += 1
                else:
                    i += 1
        except IndexError:
            pass

        return _mapper

    def parse(self, filename, encoding="utf-8", mapper_type=__OrderedDict, key_modifier=None):
        """
        Parses the KV file so this instance can be accessed by dict interface.

        :param filename: name of KV file
        :param encoding: KV file encoding. Default: 'utf-8'
        :param mapper_type: which mapper will be used for storing KV. It must have the dict interface, i.e. allow to do the 'mapper[key] = value action'.
                default: 'collections.OrderedDict'
                For example you can use the 'dict' type.
                This will override the instance's 'mapper_type' if specified during instantiation.
        :param key_modifier: function for modifying the keys, e.g. the function 'string.lower' will make all the keys lower.
                This will override the instance's 'key_modifier' if specified during instantiation.
        """

        with open(filename, mode="r", encoding=encoding) as f:
            self.__mapper = self.__parse([line.strip() for line in f.readlines()],
                                         mapper_type=mapper_type or self.mapper_type,
                                         key_modifier=key_modifier or self.key_modifier)

    def __tab(self, string, level, quotes=False):
        if quotes:
            return '{}"{}"'.format(level * "\t", string)
        else:
            return '{}{}'.format(level * "\t", string)

    def __dump(self, mapper, key_sorter=None, level=0):
        string = ""

        if key_sorter:
            keys = key_sorter(mapper.keys())
        else:
            keys = mapper.keys()

        for key in keys:
            string += self.__tab(key, level, quotes=True)
            if type(mapper[key]) == str:
                string += '\t "{}"\n'.format(mapper[key])
            else:
                string += "\n" + self.__tab("{\n", level)
                string += self.__dump(mapper[key], key_sorter=key_sorter, level=level+1)
                string += self.__tab("}\n", level)

        return string

    def dump(self, mapper=None, key_sorter=None):
        """
        Dumps the KeyValues mapper to string.

        :param mapper: you can dump your own object with dict interface
        :param key_sorter: function for sorting the keys when dumping/writing/str, e.g. using the function 'sorted' will show KV in alphabetical order.
                This will override the instance's 'key_sorter' if specified during instantiation.
        :return: string
        """

        return self.__dump(mapper=mapper or self.__mapper, key_sorter=key_sorter or self.key_sorter)

    def write(self, filename, encoding="utf-8", mapper=None, key_sorter=None):
        """
        Writes the KeyValues to file.

        :param filename: output KV file name
        :param encoding: output KV file encoding. Default: 'utf-8'
        :param mapper: you can write your own object with dict interface
        :param key_sorter: key_sorter: function for sorting the keys when dumping/writing/str, e.g. using the function 'sorted' will show KV in alphabetical order.
                This will override the instance's 'key_sorter' if specified during instantiation.
        """

        with open(filename, mode="w", encoding=encoding) as f:
            f.write(self.dump(mapper=mapper or self.__mapper, key_sorter=key_sorter or self.key_sorter))


# Set up a queue to share data between the output-reading thread and the main thread.
output_queue = queue.Queue()

def enqueue_output(out, queue):
    for line in iter(out.readline, b''):
        queue.put(line)
    out.close()

# Get the command-line arguments except the script name itself.
args = sys.argv[1:]

windows_executable = 'cs2.exe'
linux_executable = 'cs2'

# Define the executable based on the OS
executable = None
if os.name == 'nt':
    executable = windows_executable
elif os.name == 'posix':
    executable = linux_executable
else:
    print(f"Unsupported operating system: {os.name}", file=sys.stderr)
    sys.exit(1)

# So the structure of CS2 is:
# - bin/ (csgo binaries)
# - csgo/ (csgo directory)
# - game/ (cs2 directory)
# - game/bin/ (cs2 binaries)
# - game/bin/linuxsteamrt64/cs2 (linux executable)
# - game/bin/win64/cs2.exe (windows executable)
# - game/csgo/scripts/wst_launcher.py (this script)
# - game/csgo/scripts/vscript/ (vscript directory)
# - game/csgo/scripts/vscript/wst.lua (wst script)
# - game/csgo/scripts/vscript/wst-leaderboard.lua (leaderboard script)
# - game/csgo/scripts/vscript/wst_records (leaderboard data)
# - game/csgo/scripts/vscript/wst_records/surf_beginner.txt (Valve KeyValue vdffile)

# The path to the script directory should be calculated relative to the script's location
script_dir = os.path.dirname(os.path.abspath(__file__))
print(f"Script directory: {script_dir}")

root_dir = os.path.abspath(os.path.join(script_dir, '..', '..', '..'))
print(f"Root directory: {root_dir}")

records_dir = os.path.join(root_dir, 'game', 'csgo', 'scripts', 'wst_records')
print(f"Records directory: {records_dir}")

# Check if the records directory exists
if not os.path.isdir(records_dir):
    # Make the directory
    os.makedirs(records_dir)



# The executable directory is different for Windows and Linux
if os.name == 'nt':
    executable_path = os.path.join(root_dir, 'game', 'bin', 'win64', executable)
else:
    executable_path = os.path.join(root_dir, 'game', 'bin', 'linuxsteamrt64', executable)

# Check if the executable exists at the determined path
if not os.path.isfile(executable_path):
    print(f"Executable not found: {executable_path}", file=sys.stderr)
    sys.exit(1)

parsed_args = ['-dedicated']
for arg in args:
    if arg == '-dedicated':
        pass
    else:
        parsed_args.append(arg)


# Construct the command with the executable and any passed arguments
command = [executable_path] + parsed_args

full_command = ' '.join(command)
print(f"Executing: {full_command}")

# Start the subprocess and redirect the standard output and error to a pipe.
proc = subprocess.Popen(full_command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True, bufsize=1)

# Start a thread to asynchronously read the process's output and put it in the queue.
t = threading.Thread(target=enqueue_output, args=(proc.stdout, output_queue))
t.daemon = True  # Thread dies with the program.
t.start()



def process_line(line):
    if not line.startswith('[WST_MSG] map_complete'):
        return
    
    # The line should be in the format:
    # [WST_MSG] map_complete <map_name> <steam_id> <time> <player name in quotes>

    # Split the line by spaces
    split = line.split()

    # The map name is the second element
    map_name = split[2]

    # The steam id is the third element
    steam_id = split[3]

    # The time is the fourth element
    time = float(split[4])

    # The player name is the fifth element, but it's in quotes and may contain spaces
    # We can join the remaining elements with spaces to get the full name
    player_name = ' '.join(split[5:])
    # Remove the first and last characters, which should be quotes
    player_name = player_name[1:-1]

    # Load <map_name>.txt from the records directory
    records_file_path = os.path.join(records_dir, f"{map_name}.txt")

    # "Leaderboard"
    # {
    #     "version" "_1.0"
    #     "data"
    #     {
    #         "STEAM_0:1:123456"
    #         {
    #             "name" "PlayerOne"
    #             "time" "90.00"
    #         }
    #         "STEAM_0:0:654321"
    #         {
    #             "name" "PlayerTwo"
    #             "time" "120.00"
    #         }
    #     }
    # }

    # Check if the file exists
    if not os.path.isfile(records_file_path):
        kv = KeyValues()
        kv['Leaderboard'] = {
            'version': '_1.0',
            'data': {}
        }
        kv.write(records_file_path)
    
 
    kv = KeyValues(filename=records_file_path)

    # Check if the steam id is in the data
    if steam_id in kv['Leaderboard']['data']:
        # Check if the time is less than the existing time
        if time < float(kv['Leaderboard']['data'][steam_id]['time']):
            # Update the time
            kv['Leaderboard']['data'][steam_id]['time'] = str(time)
    else:
        # Add the steam id
        kv['Leaderboard']['data'][steam_id] = {
            'name': player_name,
            'time': str(time)
        }

    kv.write(records_file_path)

    print(f"Map: {map_name}, Steam ID: {steam_id}, Time: {time}, Player Name: {player_name}")
    

        
# Main loop to process the output and handle keyboard interrupts.
try:
    while True:
        # Check if the process is still running.
        if proc.poll() is not None:
            break

        try:
            # Try to get output from the queue, timeout after 1 second.
            line = output_queue.get(timeout=5)
            
            # Process the line
            process_line(line)

            print(line, end='')
        except queue.Empty:
            # No output received, the process is still running, continue the loop.
            continue

except KeyboardInterrupt:
    print("\nKeyboard interrupt received, stopping...")
    proc.terminate()
    try:
        proc.wait(timeout=5)
    except subprocess.TimeoutExpired:
        proc.kill()
    sys.exit(0)

except Exception as e:
    print(f"An error occurred: {e}", file=sys.stderr)
    proc.kill()
    sys.exit(1)

finally:
    # Cleanup any remaining threads and close the process's stdout.
    proc.stdout.close()
    proc.wait()