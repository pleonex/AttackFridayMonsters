// Copyright (c) 2020 Benito Palacios Sánchez
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
import java.io.IOException;
import java.io.FileWriter;
import java.util.List;
import java.util.HashMap;
import java.util.Iterator;

import ghidra.app.script.GhidraScript;
import ghidra.program.flatapi.FlatProgramAPI;
import ghidra.program.model.data.DataType;
import ghidra.program.model.listing.Data;
import ghidra.program.model.mem.MemoryBlock;
import ghidra.program.model.mem.MemoryBlockSourceInfo;
import ghidra.program.model.symbol.Reference;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

public class ExportDefinedStrings extends GhidraScript {
    Logger logger;
    HashMap<String, String> ghidraTypeEncoding;

    public ExportDefinedStrings()
    {
        logger = LogManager.getLogger(ExportDefinedStrings.class);

        ghidraTypeEncoding = new HashMap<String, String>();
        ghidraTypeEncoding.put("string", "ascii");
        ghidraTypeEncoding.put("TerminatedCString", "ascii");
        ghidraTypeEncoding.put("unicode", "utf-16");
        ghidraTypeEncoding.put("TerminatedUnicode", "utf-16");
    }

    @Override
    protected void run() throws IOException, Exception {
        String[] args = getScriptArgs();
        if (args.length != 1) {
            logger.error("USAGE: ExportDefinedString.java <output_file>");
            return;
        }

        String outputPath = args[0];
        logger.info("Exporting defined strings into " + outputPath);
        FileWriter outputWriter = new FileWriter(outputPath);

        FlatProgramAPI api = new FlatProgramAPI(currentProgram);
        exportMemoryMap(api.getMemoryBlocks(), outputWriter);
        exportStringDefinitions(api, outputWriter);

        outputWriter.close();
    }

    private void exportMemoryMap(MemoryBlock[] blocks, FileWriter outputWriter)
        throws IOException
    {
        outputWriter.write("offset:\n");
        for (MemoryBlock block : blocks) {
            outputWriter.write("  - name: " + block.getName() + "\n");
            outputWriter.write("    ram: 0x" + block.getStart().toString() + "\n");

            List<MemoryBlockSourceInfo> sources = block.getSourceInfos();
            if (sources.size() != 1) {
                logger.error("ERROR: Block with multiple sources");
                return;
            }

            MemoryBlockSourceInfo source = sources.get(0);
            String address = Long.toHexString(source.getFileBytesOffset());
            outputWriter.write("    file: 0x" + address + "\n");
            outputWriter.write("\n");
        }
    }

    private void exportStringDefinitions(FlatProgramAPI api, FileWriter outputWriter)
        throws IOException
    {
        outputWriter.write("definitions:\n");
        Data data = api.getFirstData();
        while (data != null) {
            DataType type = data.getDataType();
            if (ghidraTypeEncoding.containsKey(type.getName())) {
                exportString(data, outputWriter);
            }

            data = api.getDataAfter(data);
        }
    }

    private void exportString(Data data, FileWriter outputWriter)
        throws IOException
    {
        outputWriter.write("  # " + data.getValue().toString().replace("\n", "\\n") + "\n");
        outputWriter.write("  - address: 0x" + data.getAddressString(false, true) + "\n");
        outputWriter.write("    size: " + data.getLength() + "\n");

        String encoding = ghidraTypeEncoding.get(data.getDataType().getName());
        outputWriter.write("    encoding: " + encoding + "\n");
        outputWriter.write("    pointers:\n");

        for (Reference ref : data.getReferenceIteratorTo()) {
            outputWriter.write("      - 0x" + ref.getFromAddress().toString(false, true) + "\n");
        }

        outputWriter.write("\n");
    }
}
