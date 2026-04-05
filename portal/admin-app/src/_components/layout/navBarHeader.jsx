import { React, useState, useEffect } from "react";
import companyLogo from "../../_assets/images/TokenIDP.svg";

function NavBarHeader() {
  return (
    <header className="nav-header d-flex justify-content-between fixed-top">
      <a href="#">
        <img src={companyLogo} width="163" alt="TokenIDP" />
      </a>
      <button>
        <svg
          fill="currentColor"
          preserveAspectRatio="xMidYMid meet"
          height="20px"
          width="20px"
          viewBox="0 0 16 12"
          className="css-1gqb398"
        >
          <title>menu</title>
          <g>
            <path d="M.5 12h15v-1.667H.5V12zm0-4.167h15V6.167H.5v1.666zm6.667-4.166H15.5V2H7.167v1.667z"></path>
            <path d="M4.505 2.833L7.5.75v4.167z"></path>
          </g>
        </svg>
      </button>
    </header>
  );
}

export default NavBarHeader;
