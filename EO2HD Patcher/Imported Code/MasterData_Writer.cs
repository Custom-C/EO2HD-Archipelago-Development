namespace EO2HD_Patcher.Imported_Code;


/*
//Astera's EO3 MasterData writer script in python, 
//Sourced from a Discord message in the Etrian Odyssey Modding discord,
//channel #eo2-modding-chat,
//dated 2023-07-04.
 
 # This script requires an unpacked copy of EO2HD's MasterData.bin, and EO2MasterDataFilenames.txt

masterData = bytes()
fileStructure = list()
offset = list()
littleEndian = list()
root = "MasterData/"

with open('EO2MasterDataFilenames.txt', 'r') as textFile:
    fileStructure = textFile.readlines()
    textFile.close()

for item in fileStructure:
    item = item.replace("\n", "")
    with open(root + item, 'rb') as binFile:
        offset.append(len(masterData))
        masterData += binFile.read()
        binFile.close()

with open('MasterDataNew.bin', 'xb') as binFile:
    binFile.write(masterData)
    binFile.close()
print("New MasterData.bin file created as 'MasterDataNew.bin'")

# convert int to string formatted as big endian hexadecimal to little endian hexadecimal
for item in offset:
    bits = [8,8,8,8]
    b = bytearray.fromhex(f'{int(item/4):08x}')
    i = 0
    for n in bits:
        b[i:i+n] = reversed(b[i:i+n])
        i += n
    littleEndian.append(b)

with open('MasterOffsetNew.bin', 'xb') as binFile:
    for item in littleEndian:
        binFile.write(item)
    binFile.close()
print("New MasterOffset.bin file created as 'MasterOffsetNew.bin'")

dummy = input("Press enter to exit.")
*/