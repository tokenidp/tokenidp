import Breadcrumbs from "./breadcrumbs";

function PageHeader(props) {
  return (
    <div className="d-flex justify-content-between align-items-center">
      <h1 className="first-heading">{props.pageHeading}</h1>
      <div className="">
        <Breadcrumbs />
      </div>
    </div>
  );
}

export default PageHeader;
