def read_int(f, n):
    return int.from_bytes(f.read(n), 'little')

def write_int(f, x, n):
    f.write(x.to_bytes(n, 'little'))
    
def read_leb128(f):
    r = 0
    s = 0
    
    while True:
        b = f.read(1)[0]
        r |= (b & 127) << s
        s += 7
        
        if (b & 128) == 0:
            return r

def write_leb128(f, x):
    while True:
        b = x & 127
        x >>= 7
        if x != 0:
            b |= 128
        
        f.write(bytes([b]))
        
        if x == 0:
            break

def read_str(f):
    l = read_leb128(f)
    return f.read(l).replace(b'\n', b'\\n').replace(b'\r', b'\\r')

def write_str(f, s):
    s = s.replace(b'\\n', b'\n').replace(b'\\r', b'\r')
    write_leb128(f, len(s))
    f.write(s)

with open('en-US.loc', 'rb') as f, \
     open('en-US.loc.txt', 'wb') as o:
    p0 = read_int(f, 2) # 1
    o.write(read_str(f) + b'\n')
    o.write(read_str(f) + b'\n')
    o.write(read_str(f) + b'\n')
    count = read_int(f, 4) * 2 # x2; key + value
    
    # key
    # value
    # key = value
    is_value = False
    
    for _ in range(count):
        if not is_value:
            o.write(read_str(f) + b' = ')
            is_value = True
            continue
        o.write(read_str(f) + b'\n')
        is_value = False
    
    rest = f.read()

with open("th-TH.loc", 'wb') as o, \
     open('th-TH.loc.txt', 'rb') as f:
    
    write_int(o, p0, 2)
    write_str(o, f.readline()[:-1])
    write_str(o, f.readline()[:-1])
    write_str(o, f.readline()[:-1])
    
    p = f.tell()
    write_int(o, sum(1 for _ in f), 4)
    f.seek(p, 0)
    
    for line in f:
        sep = line.find(b' = ')
        if sep < 0:
            exit(1)
        
        k = line[:sep]
        v = line[sep + 3:-1]
        write_str(o, k)
        write_str(o, v)
        
    o.write(rest)