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
		FileWriter writer = new FileWriter(outputPath);
		writer.write("block:\n");

		FlatProgramAPI api = new FlatProgramAPI(currentProgram);
		Data data = api.getFirstData();
		while (data != null) {
			DataType type = data.getDataType();
			if (type.getName() == "string" || type.getName() == "unicode") {
				try {
					exportString(data, writer);
				} catch (IOException ex) {
				}
			}

			data = api.getDataAfter(data);
		}

		writer.close();
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
