import fs from "node:fs/promises";
import path from "node:path";
import { Workbook, SpreadsheetFile } from "@oai/artifact-tool";

const outputDir = path.resolve("outputs/bargaining-prototype");
const outputPath = path.join(outputDir, "Bargaining_Economy_Config.xlsx");

const workbook = Workbook.create();

const summary = workbook.worksheets.add("Summary");
summary.getRange("A1:F3").merge();
summary.getRange("A1").values = [["Bargaining Economy Prototype"]];
summary.getRange("A1:F3").format = {
  font: { size: 24, bold: true, color: "#FFFFFF" },
  horizontalAlignment: "center",
  verticalAlignment: "center",
  fill: "#1F4E5F",
};

summary.getRange("A6:B10").values = [
  ["Config", "Value"],
  ["Negotiation Stamina Cost", 8],
  ["Offer Step", 500],
  ["NPC Count", 2],
  ["Agricultural Item Count", 5],
];

summary.getRange("D6:F9").values = [
  ["NPC", "Opening Multiplier", "Max Accept Multiplier"],
  ["Merchant NPC", 0.92, 1.1],
  ["Villager NPC", 0.85, 1.02],
  ["Note", "Higher multiplier means tougher negotiation.", ""],
];

summary.getRange("A6:B6").format = {
  font: { bold: true },
  fill: "#DDEBF7",
};
summary.getRange("D6:F6").format = {
  font: { bold: true },
  fill: "#FCE4D6",
};

summary.getRange("A12:F18").values = [
  ["Prototype Flow", "", "", "", "", ""],
  ["1. Inventory Screen", "Player selects item from boat inventory", "", "", "", ""],
  ["2. Shop Menu Screen", "Player chooses Merchant or Villager NPC", "", "", "", ""],
  ["3. Bargaining Screen", "Increase or decrease asking price", "", "", "", ""],
  ["4. Stamina Rule", "Each negotiation action costs 8 stamina", "", "", "", ""],
  ["5. Result", "NPC accepts or rejects based on configured threshold", "", "", "", ""],
  ["6. Economy Outcome", "Successful deal sells 1 item and adds money", "", "", "", ""],
];
summary.getRange("A12:F12").merge();
summary.getRange("A12:F12").format = {
  font: { bold: true },
  fill: "#E2F0D9",
};

const dataSheet = workbook.worksheets.add("Economy Data");
dataSheet.getRange("A1:G7").values = [
  ["Item ID", "Item Name", "Base Price (VND)", "Min Variation", "Max Variation", "Start Qty", "Weight (kg)"],
  ["ITM_KHOM", "Khom", 15000, -2000, 4000, 3, 5],
  ["ITM_BIDAO", "Bi Dao", 18000, -2500, 4500, 3, 6],
  ["ITM_XOAI", "Xoai", 22000, -3000, 5500, 2, 4],
  ["ITM_DUAHAU", "Dua Hau", 26000, -3500, 6500, 2, 8],
  ["ITM_CAM", "Cam", 20000, -1500, 3500, 4, 3],
  ["Rule", "Every increase/decrease offer costs 8 stamina", "", "", "", "", ""],
];

dataSheet.getRange("A1:G1").format = {
  font: { bold: true },
  fill: "#FFF2CC",
};
dataSheet.getRange("A7:G7").format = {
  font: { italic: true },
  fill: "#F3F3F3",
};

summary.getRange("A:F").format.autofitColumns();
dataSheet.getRange("A:G").format.autofitColumns();

await fs.mkdir(outputDir, { recursive: true });
const output = await SpreadsheetFile.exportXlsx(workbook);
await output.save(outputPath);

console.log(outputPath);
