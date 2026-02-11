import React, { useEffect, useState } from "react";
import { useAuth } from "tokentresor-idp-react";

function PrivateButton(props) {
  const user = useAuth();
  const [isPermissionExist, setPermissionExist] = useState(false);

  useEffect(() => {
    const claim = user.permissions.find((c) => {
      return (
        c.permissionKey === props.permissionKey ||
        c.PermissionKey === props.permissionKey
      );
    });

    if (claim) {
      setPermissionExist(true);
    }
  }, []);

  return isPermissionExist ? (
    <button
      type="submit"
      className="btn btn-block app-btn "
      onClick={props.onClick}
    >
      {props.label}
    </button>
  ) : (
    <button
      type="submit"
      className="btn btn-block app-btn "
      disabled={isPermissionExist}
    >
      {props.label}
    </button>
  );
}

export default PrivateButton;
