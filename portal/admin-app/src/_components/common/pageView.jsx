import React from "react";
import { Outlet } from "react-router-dom";
import PageHeader from "./pageHeader";

function PageView(props) {
  return (
    <React.Fragment>
      <PageHeader pageHeading={props.pageHeading} />
      <Outlet />
    </React.Fragment>
  );
}

export default PageView;
