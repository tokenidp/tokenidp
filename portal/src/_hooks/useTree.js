const useTree = () => {
  const createTree = (permissions, options = {}) => {
    const { allowedControlTypes } = options;
    const data = permissions
      .map((item) => {
        const controlType = (
          item.controlType ||
          item.ControlType ||
          ""
        ).toLowerCase();
        const permissionId = item.id ?? item.Id;
        const parentId = item.parentId ?? item.ParentId ?? null;
        const sequence = item.sequence ?? item.Sequence ?? 0;
        return {
          ...item,
          controlType,
          permissionId: permissionId != null ? String(permissionId) : null,
          parentId: parentId != null ? String(parentId) : null,
          sequence: Number(sequence) || 0,
        };
      })
      .filter((c) => {
        if (allowedControlTypes === "all") {
          return true;
        }
        if (Array.isArray(allowedControlTypes)) {
          return allowedControlTypes.includes(c.controlType);
        }
        return c.controlType === "navgroup" || c.controlType === "navlink";
      });

    //Data without parent node
    let parents = data.filter(
      (value) => value.parentId == null || value.parentId === "0"
    );

    //Data with parent node
    let childrens = data.filter(
      (value) => value.parentId != null && value.parentId !== "0"
    );

    //Define the concrete implementation of transformation method
    let translator = (parents, childrens) => {
      //Traverse parent node data
      parents.forEach((parent) => {
        //Traversal of child node data
        childrens.forEach((current, index) => {
          //At this time, find a child node corresponding to the parent node
          if (current.parentId === parent.permissionId) {
            //Put the found child node in the children attribute of the parent node
            typeof parent.childrens !== "undefined"
              ? parent.childrens.push(current)
              : (parent.childrens = [current]);
            //Deep replication of sub node data is only supported here. For children's boots that don't know about deep replication, you can first learn about deep replication
            let temp = JSON.parse(JSON.stringify(childrens));
            //Let the current child node be removed from temp, which is the new data of child nodes. This is to make the number of iterations of child nodes less during recursion. The more layers of parent-child relationship, the more favorable
            temp.splice(index, 1);
            //Let the current child node be the only parent node to recursively find its corresponding child node
            translator([current], temp);
          }
        });
      });
    };

    //Call transformation method
    translator(parents, childrens);

    const sortNodes = (nodes) => {
      nodes.sort((a, b) => (a.sequence || 0) - (b.sequence || 0));
      nodes.forEach((node) => {
        if (Array.isArray(node.childrens)) {
          sortNodes(node.childrens);
        }
      });
    };

    sortNodes(parents);
    return parents;
  };
  return { createTree };
};

export default useTree;
