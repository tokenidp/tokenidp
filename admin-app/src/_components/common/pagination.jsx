import React from "react";

function Pagination({
  pageNumber,
  pageSize,
  totalCount,
  onPageChange,
  maxPageButtons = 5,
}) {
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const startEntry = totalCount === 0 ? 0 : (pageNumber - 1) * pageSize + 1;
  const endEntry = Math.min(pageNumber * pageSize, totalCount);

  const pageStart = Math.max(1, pageNumber - Math.floor(maxPageButtons / 2));
  const pageEnd = Math.min(totalPages, pageStart + maxPageButtons - 1);
  const pageNumbers = Array.from(
    { length: pageEnd - pageStart + 1 },
    (_, index) => pageStart + index
  );

  return (
    <div className="table-footer">
      <div className="text-muted small">
        Showing {startEntry} to {endEntry} of {totalCount} entries
      </div>
      <div className="pagination-controls">
        <button
          className="btn btn-light btn-sm"
          type="button"
          disabled={pageNumber <= 1}
          onClick={() => onPageChange(Math.max(1, pageNumber - 1))}
        >
          <i className="fa fa-angle-left"></i>
        </button>
        {pageNumbers.map((page) => (
          <button
            key={page}
            className={`btn btn-sm ${
              page === pageNumber ? "btn-primary" : "btn-light"
            }`}
            type="button"
            onClick={() => onPageChange(page)}
          >
            {page}
          </button>
        ))}
        <button
          className="btn btn-light btn-sm"
          type="button"
          disabled={pageNumber >= totalPages}
          onClick={() => onPageChange(Math.min(totalPages, pageNumber + 1))}
        >
          <i className="fa fa-angle-right"></i>
        </button>
      </div>
    </div>
  );
}

export default Pagination;
