"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var SupportRequest = /** @class */ (function () {
    function SupportRequest() {
    }
    return SupportRequest;
}());
exports.SupportRequest = SupportRequest;
var SupportRequestState;
(function (SupportRequestState) {
    SupportRequestState["Draft"] = "Draft";
    SupportRequestState["Submitted"] = "Submitted";
    SupportRequestState["Approved"] = "Approved";
    SupportRequestState["Posted"] = "Posted";
    SupportRequestState["Canceled"] = "Canceled";
    SupportRequestState["Rejected"] = "Rejected";
})(SupportRequestState = exports.SupportRequestState || (exports.SupportRequestState = {}));
var supportRequestReasons = {
    DC: 'Display Contract',
    PS: 'Premium Support',
    PR: 'Price Reduction',
    FB: 'From Balance'
};
exports.supportRequestReasons = supportRequestReasons;
var SupportRequestLineItem = /** @class */ (function () {
    function SupportRequestLineItem() {
        this.Quantity = 0;
        this.RequestedSupport = 0;
        this.RequestedValue = 0;
        this.ApprovedSupport = 0;
        this.ApprovedValue = 0;
        this.UsedSupport = 0;
        this.UsedValue = 0;
    }
    return SupportRequestLineItem;
}());
exports.SupportRequestLineItem = SupportRequestLineItem;
var StateChange = /** @class */ (function () {
    function StateChange() {
    }
    return StateChange;
}());
exports.StateChange = StateChange;
var GeneratedDocument = /** @class */ (function () {
    function GeneratedDocument() {
    }
    return GeneratedDocument;
}());
exports.GeneratedDocument = GeneratedDocument;
//# sourceMappingURL=SupportRequest.js.map