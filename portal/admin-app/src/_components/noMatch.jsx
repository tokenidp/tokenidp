import React from "react";
import { Link } from "react-router-dom";

function NoMatch() {
  return (
    <React.Fragment>
      <h2>Nothing to see here!</h2>
      <p>
        <Link to="/">Go to the home page of the app</Link>
      </p>
    </React.Fragment>
  );
}

export default NoMatch;
