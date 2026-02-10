import React, { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import Breadcrumbs from "../common/breadcrumbs";
import InfoModal from "../common/infoModal";
import { useUsers } from "../../_hooks/useUsers";

const DEFAULT_ADDRESS_TYPES = [
  { key: "1", value: "Home" },
  { key: "2", value: "Work" },
  { key: "3", value: "Billing" },
];

const getLookupValue = (item) =>
  item?.key ??
  item?.Key ??
  item?.id ??
  item?.Id ??
  item?.value ??
  item?.Value ??
  item?.name ??
  item?.Name ??
  "";

const getLookupLabel = (item) =>
  item?.value ??
  item?.Value ??
  item?.name ??
  item?.Name ??
  item?.key ??
  item?.Key ??
  "";

const resolveAddressTypeValue = (rawValue, options) => {
  if (rawValue === undefined || rawValue === null || rawValue === "") {
    return "";
  }
  const rawString = String(rawValue);
  if (rawString === "0" && options?.length) {
    return String(getLookupValue(options[0]));
  }
  const matched = (options || []).find((option) => {
    const optionValue = String(getLookupValue(option));
    const optionLabel = String(getLookupLabel(option));
    return optionValue === rawString || optionLabel === rawString;
  });
  if (matched) {
    return String(getLookupValue(matched));
  }
  const rawNumber = Number(rawValue);
  if (!Number.isNaN(rawNumber) && rawNumber > 0) {
    const candidate = options?.[rawNumber - 1];
    if (candidate) {
      return String(getLookupValue(candidate));
    }
  }
  return rawString;
};

function AddEditUser({ mode }) {
  const navigate = useNavigate();
  const params = useParams();
  const userId = params.userId;
  const { state, loadLookups, getUserById, createUser, updateUser } = useUsers();
  const [currentUserId, setCurrentUserId] = useState(
    userId ? Number(userId) : 0
  );
  const [activeTab, setActiveTab] = useState("details");
  const [showPassword, setShowPassword] = useState(false);
  const [showMetadata, setShowMetadata] = useState(false);
  const [infoOpen, setInfoOpen] = useState(false);
  const [infoContent, setInfoContent] = useState({ title: "", message: "" });

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm({
    defaultValues: {
      firstName: "",
      lastName: "",
      userName: "",
      normalizedUserName: "",
      email: "",
      phone: "",
      password: "",
      status: "",
      emailConfirmed: false,
      phoneNumberConfirmed: false,
      twoFactorEnabled: false,
      lockoutEnabled: false,
      accessFailedCount: 0,
      lockoutEnd: "",
      securityStamp: "",
      concurrencyStamp: "",
      addressType: "",
      addressLine1: "",
      addressLine2: "",
      city: "",
      state: "",
      postalCode: "",
      country: "",
      contactType: "",
      contactRelationship: "",
      contactEmail: "",
      contactPhoneNumber: "",
      contactAddressLine1: "",
      contactAddressLine2: "",
      contactCity: "",
      contactState: "",
      contactPostalCode: "",
      contactCountry: "",
      roles: [],
    },
  });

  const selectedRoles = watch("roles") || [];
  const lockoutEnabled = watch("lockoutEnabled");
  const emailConfirmed = watch("emailConfirmed");
  const phoneNumberConfirmed = watch("phoneNumberConfirmed");
  const securityStamp = watch("securityStamp");
  const concurrencyStamp = watch("concurrencyStamp");
  const addressType = watch("addressType");
  const contactType = watch("contactType");
  const contactRelationship = watch("contactRelationship");
  const contactEmail = watch("contactEmail");
  const contactPhoneNumber = watch("contactPhoneNumber");
  const contactAddressLine1 = watch("contactAddressLine1");
  const contactAddressLine2 = watch("contactAddressLine2");
  const contactCity = watch("contactCity");
  const contactState = watch("contactState");
  const contactPostalCode = watch("contactPostalCode");
  const contactCountry = watch("contactCountry");
  const addressTypeOptions = state.addressTypes?.length
    ? state.addressTypes
    : DEFAULT_ADDRESS_TYPES;
  const [addressTypeSeed, setAddressTypeSeed] = useState(null);
  const hasContactDetails = [
    contactType,
    contactRelationship,
    contactEmail,
    contactPhoneNumber,
    contactAddressLine1,
    contactAddressLine2,
    contactCity,
    contactState,
    contactPostalCode,
    contactCountry,
  ].some((value) => String(value || "").trim().length > 0);

  useEffect(() => {
    loadLookups();
  }, [loadLookups]);

  useEffect(() => {
    if (!userId || mode !== "edit") return;
    const loadUser = async () => {
      const data = await getUserById(userId);
      if (!data) return;
      const resolvedId =
        data.id ?? data.Id ?? (Number(userId) || 0);
      setCurrentUserId(resolvedId);
      setValue("firstName", data.firstName ?? data.FirstName ?? "");
      setValue("lastName", data.lastName ?? data.LastName ?? "");
      setValue("userName", data.userName ?? data.UserName ?? "");
      setValue(
        "normalizedUserName",
        data.normalizedUserName ?? data.NormalizedUserName ?? ""
      );
      setValue("email", data.email ?? data.Email ?? "");
      setValue("phone", data.phone ?? data.Phone ?? "");
      setValue("status", data.statusId ?? data.StatusId ?? data.status ?? "");
      setValue(
        "emailConfirmed",
        data.emailConfirmed ?? data.EmailConfirmed ?? false
      );
      setValue(
        "phoneNumberConfirmed",
        data.phoneNumberConfirmed ?? data.PhoneNumberConfirmed ?? false
      );
      setValue(
        "twoFactorEnabled",
        data.twoFactorEnabled ?? data.TwoFactorEnabled ?? false
      );
      setValue(
        "lockoutEnabled",
        data.lockoutEnabled ?? data.LockoutEnabled ?? false
      );
      setValue(
        "accessFailedCount",
        data.accessFailedCount ?? data.AccessFailedCount ?? 0
      );
      const lockoutEnd =
        data.lockoutEnd ?? data.LockoutEnd ?? data.lockoutEndDate ?? "";
      setValue(
        "lockoutEnd",
        lockoutEnd ? String(lockoutEnd).slice(0, 16) : ""
      );
      setValue(
        "securityStamp",
        data.securityStamp ?? data.SecurityStamp ?? ""
      );
      setValue(
        "concurrencyStamp",
        data.concurrencyStamp ?? data.ConcurrencyStamp ?? ""
      );
      setValue("roles", data.roles ?? data.Roles ?? []);
      console.log("address object:", data.addresses ?? data.Addresses ?? []);
      const addresses = data.addresses ?? data.Addresses ?? [];
      const primaryAddress = addresses[0] || {};
      const rawAddressType =
        primaryAddress.addressType ?? primaryAddress.AddressType ?? "";
      setAddressTypeSeed(rawAddressType);
      setValue(
        "addressType",
        resolveAddressTypeValue(rawAddressType, addressTypeOptions)
      );
      setValue(
        "addressLine1",
        primaryAddress.addressLine1 ?? primaryAddress.AddressLine1 ?? ""
      );
      setValue(
        "addressLine2",
        primaryAddress.addressLine2 ?? primaryAddress.AddressLine2 ?? ""
      );
      setValue("city", primaryAddress.city ?? primaryAddress.City ?? "");
      setValue("state", primaryAddress.state ?? primaryAddress.State ?? "");
      setValue(
        "postalCode",
        primaryAddress.postalCode ?? primaryAddress.PostalCode ?? ""
      );
      setValue(
        "country",
        primaryAddress.country ?? primaryAddress.Country ?? ""
      );

      const contacts = data.contacts ?? data.Contacts ?? [];
      const primaryContact = contacts[0] || {};
      setValue(
        "contactType",
        primaryContact.contactType ?? primaryContact.ContactType ?? ""
      );
      setValue(
        "contactRelationship",
        primaryContact.relationship ?? primaryContact.Relationship ?? ""
      );
      setValue(
        "contactEmail",
        primaryContact.email ?? primaryContact.Email ?? ""
      );
      setValue(
        "contactPhoneNumber",
        primaryContact.phoneNumber ?? primaryContact.PhoneNumber ?? ""
      );
      setValue(
        "contactAddressLine1",
        primaryContact.addressLine1 ?? primaryContact.AddressLine1 ?? ""
      );
      setValue(
        "contactAddressLine2",
        primaryContact.addressLine2 ?? primaryContact.AddressLine2 ?? ""
      );
      setValue(
        "contactCity",
        primaryContact.city ?? primaryContact.City ?? ""
      );
      setValue(
        "contactState",
        primaryContact.state ?? primaryContact.State ?? ""
      );
      setValue(
        "contactPostalCode",
        primaryContact.postalCode ?? primaryContact.PostalCode ?? ""
      );
      setValue(
        "contactCountry",
        primaryContact.country ?? primaryContact.Country ?? ""
      );
    };
    loadUser();
  }, [addressTypeOptions, getUserById, mode, setValue, userId]);

  useEffect(() => {
    if (!userId || mode !== "edit") return;
    const resolvedId = Number(userId) || 0;
    if (resolvedId && resolvedId !== currentUserId) {
      setCurrentUserId(resolvedId);
    }
  }, [currentUserId, mode, userId]);

  useEffect(() => {
    if (mode === "edit" && userId && addressTypeSeed !== null) {
      setValue(
        "addressType",
        resolveAddressTypeValue(addressTypeSeed, addressTypeOptions)
      );
    }
  }, [addressTypeOptions, addressTypeSeed, mode, setValue, userId]);

  useEffect(() => {
    if (mode !== "add") return;
    if (addressType || addressTypeOptions.length === 0) return;
    const firstOptionValue = resolveAddressTypeValue(
      getLookupValue(addressTypeOptions[0]),
      addressTypeOptions
    );
    if (firstOptionValue) {
      setValue("addressType", firstOptionValue);
    }
  }, [addressType, addressTypeOptions, mode, setValue]);

  const toggleRole = (roleId) => {
    const value = Number(roleId);
    const next = selectedRoles.includes(value)
      ? selectedRoles.filter((item) => item !== value)
      : [...selectedRoles, value];
    setValue("roles", next, { shouldDirty: true, shouldValidate: true });
  };

  const onSubmit = async (data) => {
    const addressPayload = {
      addressType: data.addressType,
      addressLine1: data.addressLine1.trim(),
      addressLine2: data.addressLine2.trim() || null,
      city: data.city.trim(),
      state: data.state.trim() || null,
      postalCode: data.postalCode.trim() || null,
      country: data.country.trim(),
      isActive: true,
    };

    const contactsPayload = hasContactDetails
      ? [
          {
            contactType: data.contactType.trim(),
            relationship: data.contactRelationship.trim() || null,
            email: data.contactEmail.trim() || null,
            phoneNumber: data.contactPhoneNumber.trim() || null,
            addressLine1: data.contactAddressLine1.trim() || null,
            addressLine2: data.contactAddressLine2.trim() || null,
            city: data.contactCity.trim() || null,
            state: data.contactState.trim() || null,
            postalCode: data.contactPostalCode.trim() || null,
            country: data.contactCountry.trim() || null,
            isActive: true,
          },
        ]
      : [];

    const resolvedUserId = currentUserId || (userId ? Number(userId) : 0);
    const payload = {
      id: resolvedUserId,
      Id: resolvedUserId,
      tenantId: 0,
      firstName: data.firstName.trim(),
      lastName: data.lastName.trim(),
      userName: data.userName.trim(),
      email: data.email.trim(),
      phone: data.phone.trim(),
      password: data.password ? data.password : null,
      twoFactorEnabled: !!data.twoFactorEnabled,
      lockoutEnabled: !!data.lockoutEnabled,
      accessFailedCount: Number(data.accessFailedCount || 0),
      lockoutEnd: data.lockoutEnd ? new Date(data.lockoutEnd).toISOString() : null,
      addresses: [addressPayload],
      contacts: contactsPayload,
      roles: data.roles || [],
      status: data.status || null,
    };

    if (mode === "edit" && userId) {
      await updateUser(resolvedUserId, payload);
      setInfoContent({
        title: "User updated",
        message: "User updated successfully.",
      });
    } else {
      await createUser(payload);
      setInfoContent({
        title: "User saved",
        message: "User created successfully.",
      });
    }

    setInfoOpen(true);
  };

  return (
    <div className="applications-page">
      <div className="page-header">
        <div className="page-title-group">
          <h5 className="page-title mb-1">
            {mode === "add" ? "Create User" : "Edit User"}
          </h5>
          <Breadcrumbs className="app-breadcrumb mb-0" />
        </div>
      </div>

      <div className="card-surface form-surface">
        <div className="d-flex justify-content-between align-items-start mb-4">
          <div>
            <h5 className="mb-1">
              {mode === "add" ? "Create User" : "Edit User"}
            </h5>
            <div className="text-muted small">
              Manage identity profile, access status, and security settings.
            </div>
          </div>
        </div>
        {state.error && (
          <div className="text-danger mb-3">{state.error}</div>
        )}

        <ul className="nav nav-tabs app-tabs">
          <li className="nav-item">
            <button
              className={`nav-link ${activeTab === "details" ? "active" : ""}`}
              type="button"
              onClick={() => setActiveTab("details")}
            >
              User Details
            </button>
          </li>
          <li className="nav-item">
            <button
              className={`nav-link ${activeTab === "address" ? "active" : ""}`}
              type="button"
              onClick={() => setActiveTab("address")}
            >
              User Address
            </button>
          </li>
          <li className="nav-item">
            <button
              className={`nav-link ${activeTab === "contacts" ? "active" : ""}`}
              type="button"
              onClick={() => setActiveTab("contacts")}
            >
              User Contacts
            </button>
          </li>
        </ul>

        <form onSubmit={handleSubmit(onSubmit)}>
          <div className="tab-content pt-4">
            {activeTab === "details" && (
              <div className="tab-pane active">
                <div className="row g-4">
                  <div className="col-12">
                    <div className="card">
                      <div className="card-body">
                        <h6 className="card-title">Account Status</h6>
                        <div className="row g-3">
                          <div className="col-12 col-md-6">
                            <label className="form-label">Status</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-circle-dot"></i>
                              </span>
                              <select
                                className={`form-select${
                                  errors.status ? " is-invalid" : ""
                                }`}
                                {...register("status", {
                                  required: "Status is required.",
                                })}
                              >
                                <option value="">Select Status</option>
                                {state.statuses.map((status) => (
                                  <option
                                    key={status.key}
                                    value={
                                      status.value
                                    }
                                  >
                                    {status.value}
                                  </option>
                                ))}
                              </select>
                            </div>
                            {errors.status && (
                              <div className="error-msg">
                                {errors.status.message}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Lockout Enabled</label>
                            <div className="form-check form-switch app-switch account-status-switch">
                              <input
                                className="form-check-input app-switch-input"
                                type="checkbox"
                                {...register("lockoutEnabled")}
                              />
                              <label className="form-check-label">Enabled</label>
                            </div>
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Access Failed Count</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-triangle-exclamation"></i>
                              </span>
                              <input
                                className="form-control"
                                type="number"
                                min="0"
                                {...register("accessFailedCount")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Lockout End</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-calendar"></i>
                              </span>
                              <input
                                className="form-control"
                                type="datetime-local"
                                disabled={!lockoutEnabled}
                                readOnly={!lockoutEnabled}
                                {...register("lockoutEnd")}
                              />
                            </div>
                            <div className="form-text">
                              Set a lockout end time when lockout is enabled.
                            </div>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="col-12">
                    <div className="card">
                      <div className="card-body">
                        <h6 className="card-title">Identity Information</h6>
                        <div className="row g-3">
                          <div className="col-12 col-md-6">
                            <label className="form-label">First Name</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-user"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.firstName ? " is-invalid" : ""
                                }`}
                                type="text"
                                placeholder="Ava"
                                {...register("firstName", {
                                  required: "First name is required.",
                                })}
                              />
                            </div>
                            {errors.firstName && (
                              <div className="error-msg">
                                {errors.firstName.message}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Last Name</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-user"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.lastName ? " is-invalid" : ""
                                }`}
                                type="text"
                                placeholder="Patel"
                                {...register("lastName", {
                                  required: "Last name is required.",
                                })}
                              />
                            </div>
                            {errors.lastName && (
                              <div className="error-msg">
                                {errors.lastName.message}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Username</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-at"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.userName ? " is-invalid" : ""
                                }`}
                                type="text"
                                placeholder="ava.patel"
                                readOnly={mode === "edit"}
                                {...register("userName", {
                                  required: "Username is required.",
                                })}
                              />
                            </div>
                            {errors.userName && (
                              <div className="error-msg">
                                {errors.userName.message}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Normalized Username</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-id-badge"></i>
                              </span>
                              <input
                                className="form-control"
                                type="text"
                                readOnly
                                {...register("normalizedUserName")}
                              />
                            </div>
                            <div className="form-text">System generated.</div>
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Email Address</label>
                            <div className="d-flex align-items-center gap-2">
                              <div className="input-group">
                                <span className="input-group-text">
                                  <i className="fa fa-envelope"></i>
                                </span>
                                <input
                                  className={`form-control${
                                    errors.email ? " is-invalid" : ""
                                  }`}
                                  type="email"
                                  placeholder="ava@company.com"
                                  {...register("email", {
                                    required: "Email is required.",
                                  })}
                                />
                              </div>
                              <span
                                className={`badge ${
                                  emailConfirmed ? "bg-success" : "bg-secondary"
                                }`}
                              >
                                {emailConfirmed ? "Confirmed" : "Unconfirmed"}
                              </span>
                            </div>
                            {errors.email && (
                              <div className="error-msg">
                                {errors.email.message}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Phone Number</label>
                            <div className="d-flex align-items-center gap-2">
                              <div className="input-group">
                                <span className="input-group-text">
                                  <i className="fa fa-phone"></i>
                                </span>
                                <input
                                  className={`form-control${
                                    errors.phone ? " is-invalid" : ""
                                  }`}
                                  type="tel"
                                  placeholder="+1 555-0100"
                                  {...register("phone", {
                                    required: "Phone number is required.",
                                  })}
                                />
                              </div>
                              <span
                                className={`badge ${
                                  phoneNumberConfirmed
                                    ? "bg-success"
                                    : "bg-secondary"
                                }`}
                              >
                                {phoneNumberConfirmed ? "Confirmed" : "Unconfirmed"}
                              </span>
                            </div>
                            {errors.phone && (
                              <div className="error-msg">
                                {errors.phone.message}
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="col-12">
                    <div className="card">
                      <div className="card-body">
                        <h6 className="card-title">
                          Security &amp; Authentication Settings
                        </h6>
                        <div className="row g-3 align-items-center">
                          <div className="col-12 col-md-6">
                            <label className="form-label">
                              Two-Factor Authentication
                            </label>
                            <div className="form-check form-switch app-switch">
                              <input
                                className="form-check-input app-switch-input"
                                type="checkbox"
                                {...register("twoFactorEnabled")}
                              />
                              <label className="form-check-label">Enabled</label>
                            </div>
                            <div className="form-text">
                              Enabling MFA requires users to confirm a second factor.
                            </div>
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Password</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-key"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.password ? " is-invalid" : ""
                                }`}
                                type={showPassword ? "text" : "password"}
                                placeholder={
                                  mode === "add" ? "Set a password" : "Leave blank to keep"
                                }
                                {...register("password", {
                                  validate: (value) => {
                                    if (mode === "add" && !value) {
                                      return "Password is required.";
                                    }
                                    return true;
                                  },
                                })}
                              />
                              <button
                                className="btn btn-outline-secondary"
                                type="button"
                                onClick={() => setShowPassword((prev) => !prev)}
                                aria-label={
                                  showPassword ? "Hide password" : "Show password"
                                }
                              >
                                <i
                                  className={`fa ${
                                    showPassword ? "fa-eye-slash" : "fa-eye"
                                  }`}
                                ></i>
                              </button>
                            </div>
                            {errors.password && (
                              <div className="error-msg">
                                {errors.password.message}
                              </div>
                            )}
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="col-12">
                    <div className="card">
                      <div className="card-body">
                        <h6 className="card-title">Roles</h6>
                        <input
                          type="hidden"
                          {...register("roles", {
                            validate: (value) =>
                              value?.length > 0 || "Select at least one role.",
                          })}
                        />
                        <div className="row g-3">
                          {state.roles.length === 0 && (
                            <div className="text-muted">No roles available.</div>
                          )}
                          {state.roles.map((role) => {
                            const roleId = role.key ?? role.id ?? role.Id;
                            const roleName =
                              role.value ?? role.name ?? role.Name ?? "Role";
                            return (
                              <div key={roleId} className="col-12 col-md-6">
                                <div
                                  className={`option-card d-flex align-items-center gap-3 ${
                                    selectedRoles.includes(Number(roleId))
                                      ? "option-card-active"
                                      : ""
                                  }`}
                                >
                                  <input
                                    className="form-check-input mt-0"
                                    type="checkbox"
                                    id={`role-${roleId}`}
                                    checked={selectedRoles.includes(Number(roleId))}
                                    onChange={() => toggleRole(roleId)}
                                  />
                                  <label
                                    className="form-check-label w-100"
                                    htmlFor={`role-${roleId}`}
                                  >
                                    {roleName}
                                  </label>
                                </div>
                              </div>
                            );
                          })}
                        </div>
                        {errors.roles && (
                          <div className="error-msg mt-2">
                            {errors.roles.message}
                          </div>
                        )}
                        <div className="form-text mt-2">
                          Assign one or more roles to control access permissions.
                        </div>
                      </div>
                    </div>
                  </div>

                  <div className="col-12">
                    <div className="card">
                      <div className="card-body">
                        <div className="d-flex justify-content-between align-items-center">
                          <h6 className="card-title mb-0">System Metadata</h6>
                          <button
                            className="btn btn-link p-0"
                            type="button"
                            onClick={() => setShowMetadata((open) => !open)}
                          >
                            {showMetadata ? "Hide" : "Show"} details
                          </button>
                        </div>
                        {showMetadata && (
                          <div className="row g-3 mt-2">
                            <div className="col-12 col-md-6">
                              <label className="form-label">Security Stamp</label>
                              <input
                                className="form-control"
                                type="text"
                                value={securityStamp || ""}
                                readOnly
                              />
                            </div>
                            <div className="col-12 col-md-6">
                              <label className="form-label">Concurrency Stamp</label>
                              <input
                                className="form-control"
                                type="text"
                                value={concurrencyStamp || ""}
                                readOnly
                              />
                            </div>
                            <div className="col-12">
                              <div className="text-muted small">
                                System managed values used for security and concurrency.
                              </div>
                            </div>
                          </div>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {activeTab === "address" && (
              <div className="tab-pane active">
                <div className="row g-4">
                  <div className="col-12">
                    <div className="card">
                      <div className="card-body">
                        <h6 className="card-title">User Address</h6>
                        <div className="row g-3">
                          <div className="col-12 col-md-6">
                            <label className="form-label">Address Type</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-tag"></i>
                              </span>
                              <select
                                className={`form-select${
                                  errors.addressType ? " is-invalid" : ""
                                }`}
                                {...register("addressType", {
                                  required: "Address type is required.",
                                })}
                              >
                                {addressTypeOptions.map((option) => {
                                  const value = getLookupValue(option);
                                  const label = getLookupLabel(option);
                                  return (
                                    <option key={value} value={String(value)}>
                                      {label}
                                    </option>
                                  );
                                })}
                              </select>
                            </div>
                            {errors.addressType && (
                              <div className="error-msg">
                                {errors.addressType.message}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Address Line 1</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-map-marker-alt"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.addressLine1 ? " is-invalid" : ""
                                }`}
                                type="text"
                                placeholder="123 Main Street"
                                {...register("addressLine1", {
                                  required: "Address line 1 is required.",
                                })}
                              />
                            </div>
                            {errors.addressLine1 && (
                              <div className="error-msg">
                                {errors.addressLine1.message}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Address Line 2</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-road"></i>
                              </span>
                              <input
                                className="form-control"
                                type="text"
                                placeholder="Suite 500"
                                {...register("addressLine2")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-4">
                            <label className="form-label">City</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-city"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.city ? " is-invalid" : ""
                                }`}
                                type="text"
                                placeholder="Seattle"
                                {...register("city", {
                                  required: "City is required.",
                                })}
                              />
                            </div>
                            {errors.city && (
                              <div className="error-msg">{errors.city.message}</div>
                            )}
                          </div>
                          <div className="col-12 col-md-4">
                            <label className="form-label">State / Province</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-map"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.state ? " is-invalid" : ""
                                }`}
                                type="text"
                                placeholder="WA"
                                {...register("state")}
                              />
                            </div>
                            {errors.state && (
                              <div className="error-msg">{errors.state.message}</div>
                            )}
                          </div>
                          <div className="col-12 col-md-4">
                            <label className="form-label">Postal Code</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-mail-bulk"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.postalCode ? " is-invalid" : ""
                                }`}
                                type="text"
                                placeholder="98101"
                                {...register("postalCode")}
                              />
                            </div>
                            {errors.postalCode && (
                              <div className="error-msg">
                                {errors.postalCode.message}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-4">
                            <label className="form-label">Country</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-flag"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.country ? " is-invalid" : ""
                                }`}
                                type="text"
                                placeholder="United States"
                                {...register("country", {
                                  required: "Country is required.",
                                })}
                              />
                            </div>
                            {errors.country && (
                              <div className="error-msg">
                                {errors.country.message}
                              </div>
                            )}
                          </div>
                        </div>
                        <div className="form-text mt-2">
                          Primary address used for account notifications and compliance checks.
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}

            {activeTab === "contacts" && (
              <div className="tab-pane active">
                <div className="row g-4">
                  <div className="col-12">
                    <div className="card">
                      <div className="card-body">
                        <h6 className="card-title">Contact Information</h6>
                        <div className="row g-3">
                          <div className="col-12 col-md-6">
                            <label className="form-label">Contact Type</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-user-plus"></i>
                              </span>
                              <input
                                className={`form-control${
                                  errors.contactType ? " is-invalid" : ""
                                }`}
                                type="text"
                                placeholder="Emergency"
                                {...register("contactType", {
                                  validate: (value) => {
                                    if (hasContactDetails && !value.trim()) {
                                      return "Contact type is required.";
                                    }
                                    return true;
                                  },
                                })}
                              />
                            </div>
                            {errors.contactType && (
                              <div className="error-msg">
                                {errors.contactType.message}
                              </div>
                            )}
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Relationship</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-user-group"></i>
                              </span>
                              <input
                                className="form-control"
                                type="text"
                                placeholder="Spouse"
                                {...register("contactRelationship")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Contact Email</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-envelope"></i>
                              </span>
                              <input
                                className="form-control"
                                type="email"
                                placeholder="contact@company.com"
                                {...register("contactEmail")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Contact Phone</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-phone-alt"></i>
                              </span>
                              <input
                                className="form-control"
                                type="tel"
                                placeholder="+1 555-0133"
                                {...register("contactPhoneNumber")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Address Line 1</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-location-dot"></i>
                              </span>
                              <input
                                className="form-control"
                                type="text"
                                placeholder="456 Pine Street"
                                {...register("contactAddressLine1")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Address Line 2</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-road"></i>
                              </span>
                              <input
                                className="form-control"
                                type="text"
                                placeholder="Suite 200"
                                {...register("contactAddressLine2")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-4">
                            <label className="form-label">City</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-city"></i>
                              </span>
                              <input
                                className="form-control"
                                type="text"
                                placeholder="Seattle"
                                {...register("contactCity")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-4">
                            <label className="form-label">State / Province</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-map"></i>
                              </span>
                              <input
                                className="form-control"
                                type="text"
                                placeholder="WA"
                                {...register("contactState")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-4">
                            <label className="form-label">Postal Code</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-mail-bulk"></i>
                              </span>
                              <input
                                className="form-control"
                                type="text"
                                placeholder="98101"
                                {...register("contactPostalCode")}
                              />
                            </div>
                          </div>
                          <div className="col-12 col-md-6">
                            <label className="form-label">Country</label>
                            <div className="input-group">
                              <span className="input-group-text">
                                <i className="fa fa-flag"></i>
                              </span>
                              <input
                                className="form-control"
                                type="text"
                                placeholder="United States"
                                {...register("contactCountry")}
                              />
                            </div>
                          </div>
                        </div>
                        <div className="form-text mt-2">
                          Add contact details for account recovery or emergency use.
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>

          <div className="d-flex justify-content-end gap-2 mt-4">
            <button
              className="btn btn-outline-secondary"
              type="button"
              onClick={() => navigate(-1)}
            >
              Cancel
            </button>
            <button className="btn btn-primary-solid" type="submit">
              {mode === "edit" ? "Save Changes" : "Create User"}
            </button>
          </div>
        </form>
      </div>

      <InfoModal
        open={infoOpen}
        title={infoContent.title}
        message={infoContent.message}
        onClose={() => setInfoOpen(false)}
      />
    </div>
  );
}

export default AddEditUser;
