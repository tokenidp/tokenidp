import React from "react";
import { Link } from "react-router-dom";

function UnAuthorized() {
  return (
    <React.Fragment>
      <h2>UnAuthorized</h2>
      <p>
        <Link to="/">You do not have permission to view the page</Link>
      </p>
    </React.Fragment>
  );
}

export default UnAuthorized;
