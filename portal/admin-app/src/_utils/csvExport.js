const normalizeCellValue = (value) => {
  if (value === undefined || value === null) {
    return "";
  }

  if (Array.isArray(value)) {
    return value.map((item) => normalizeCellValue(item)).join(", ");
  }

  if (typeof value === "object") {
    return JSON.stringify(value);
  }

  return String(value);
};

const escapeCsvValue = (value) => {
  const normalized = normalizeCellValue(value).replace(/\r?\n/g, " ");
  const escaped = normalized.replace(/"/g, '""');
  return /[",\n]/.test(escaped) ? `"${escaped}"` : escaped;
};

export const downloadCsv = (filename, columns, rows) => {
  if (!Array.isArray(columns) || columns.length === 0 || !Array.isArray(rows)) {
    return false;
  }

  const headerLine = columns.map((column) => escapeCsvValue(column.header)).join(",");
  const dataLines = rows.map((row) =>
    columns.map((column) => escapeCsvValue(column.accessor(row))).join(","),
  );
  const csvContent = [headerLine, ...dataLines].join("\r\n");
  const blob = new Blob([`\uFEFF${csvContent}`], {
    type: "text/csv;charset=utf-8;",
  });
  const link = document.createElement("a");
  const url = window.URL.createObjectURL(blob);

  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  window.URL.revokeObjectURL(url);

  return true;
};
