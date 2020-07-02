//  Copyright (c) 2020 Benito Palacios Sánchez
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
import java.util.Iterator;

import ghidra.app.script.GhidraScript;
import ghidra.program.flatapi.FlatProgramAPI;
import ghidra.program.model.data.DataType;
import ghidra.program.model.listing.Data;
import ghidra.program.model.symbol.Reference;

public class ExportDefinedStrings extends GhidraScript {

    @Override
    protected void run() throws Exception {
        String[] args = getScriptArgs();
        if (args.length != 1) {
            System.out.println("USAGE: ExportDefinedString.java <output_file>");
            return;
        }

        String outputPath = args[0];
        System.out.println("Exporting defined strings into " + outputPath);
        FileWriter outputWriter = new FileWriter(outputPath);
        outputWriter.Write("offset: \n");
        outputWriter.write("definitions:\n");

        FlatProgramAPI api = new FlatProgramAPI(currentProgram);
        Data data = api.getFirstData();
        while (data != null) {
            DataType type = data.getDataType();
            if (type.getName() == "string" || type.getName() == "unicode") {
                try {
                    exportString(data, outputWriter);
                } catch (IOException ex) {
                }
            }

            data = api.getDataAfter(data);
        }

        outputWriter.close();
        System.out.println("Done!");
    }

    private static void exportString(Data data, FileWriter writer)
        throws IOException
    {
        writer.write("  # " + data.getValue().toString().replace("\n", "\\n") + "\n");
        writer.write("  - address: 0x" + data.getAddressString(false, true) + "\n");
        writer.write("    size: " + data.getLength() + "\n");

        String encoding = (data.getDataType().getName() == "unicode") ? "utf-16" : "ascii";
        writer.write("    encoding: " + encoding + "\n");
        writer.write("    pointers:\n");

        for (Reference ref : data.getReferenceIteratorTo()) {
            writer.write("      - 0x" + ref.getFromAddress().toString(false, true) + "\n");
        }

        writer.write("\n");
    }
}
