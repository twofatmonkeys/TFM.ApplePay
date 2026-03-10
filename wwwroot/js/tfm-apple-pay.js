var TFM = TFM || {};
TFM.ApplePay = TFM.ApplePay || {};

document.addEventListener('DOMContentLoaded', function () {
    function initApplePay(el) {
        // Guard against double-init
        if (el._applePayInitialized) return;
        el._applePayInitialized = true;

        var config = JSON.parse(el.getAttribute('data-apple-pay'));
        TFM.ApplePay.init(config);
    }

    // Init any already in the DOM
    document.querySelectorAll('[data-apple-pay]').forEach(initApplePay);

    // Watch for dynamically added elements
    var observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeType !== Node.ELEMENT_NODE) return;

                // Check the node itself
                if (node.hasAttribute && node.hasAttribute('data-apple-pay')) {
                    initApplePay(node);
                }

                // Check descendants (e.g. a wrapper div was injected containing the element)
                if (node.querySelectorAll) {
                    node.querySelectorAll('[data-apple-pay]').forEach(initApplePay);
                }
            });
        });
    });

    observer.observe(document.body, { childList: true, subtree: true });
});

TFM.ApplePay.init = function (config) {

    config.log = function (message, error) {
        var isError = error !== undefined && error !== null;
        if (isError) {
            console.error(message, error);
        } else {
            console.log(message);
        }
        if (config.logMessageUrl) {
            fetch(config.logMessageUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    message: message,
                    error: isError
                        ? (error instanceof Error ? error.name + ': ' + error.message : String(error))
                        : null
                })
            }).catch(function (err) {
                console.error('Failed to send log:', err);
            });
        }
    };

    if (!window.ApplePaySession) {
        config.log('Apple Pay is not available on this device/browser.');
        var el = document.getElementById(config.elementId);
        if (el) el.style.display = 'none';
        return;
    }
    if (!ApplePaySession.canMakePayments()) {
        config.log('Apple Pay is supported but no cards are configured.');
        var el = document.getElementById(config.elementId);
        if (el) el.style.display = 'none';
        return;
    }
    var container = document.getElementById(config.elementId);
    var button = container.querySelector('apple-pay-button');
    if (button) {
        button.addEventListener('click', function () {
            TFM.ApplePay.startSession(config);
        });
    }
};

TFM.ApplePay.startSession = function (config) {
    config.log('Apple Pay session started');
    var paymentRequest = {
        countryCode: config.countryCode,
        currencyCode: config.currencyCode,
        supportedNetworks: config.supportedNetworks,
        merchantCapabilities: config.merchantCapabilities,
        requiredBillingContactFields: config.requiredBillingContactFields,
        requiredShippingContactFields: config.requiredShippingContactFields,
        applicationData: config.applicationData,
        total: config.total
    };

    if (config.lineItems) {
        paymentRequest.lineItems = typeof config.lineItems === 'string'
            ? JSON.parse(config.lineItems)
            : config.lineItems;
    }

    var session = new ApplePaySession(config.apiVersion, paymentRequest);

    session.onvalidatemerchant = function (event) {
        fetch(config.validateMerchantUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ validationUrl: event.validationURL })
        })
        .then(function (response) {
            if (!response.ok) {
                config.log('Merchant validation request failed with status: ' + response.status);
                throw new Error('Merchant validation request failed: ' + response.status);
            }
            return response.json();
        })
        .then(function (merchantSession) {
            session.completeMerchantValidation(merchantSession);
        })
        .catch(function (err) {
            config.log('Merchant validation failed:', err);
            session.abort();
            if (config.onError && typeof window[config.onError] === 'function') {
                window[config.onError](err);
            }
        });
    };

    session.onpaymentauthorized = function (event) {
        fetch(config.processPaymentUrl, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                token: event.payment.token,
                billingContact: event.payment.billingContact,
                shippingContact: event.payment.shippingContact,
                paymentRequest: paymentRequest
            })
        })
        .then(function (response) { return response.json(); })
        .then(function (result) {
            session.completePayment(result);
            if (result.status === ApplePaySession.STATUS_SUCCESS) {
                if (config.onSuccess && typeof window[config.onSuccess] === 'function') {
                    window[config.onSuccess](result);
                }
            } else {
                if (config.onError && typeof window[config.onError] === 'function') {
                    window[config.onError](result);
                }
            }
        })
        .catch(function (err) {
            config.log('Payment processing failed:', err);
            session.completePayment(ApplePaySession.STATUS_FAILURE);
            if (config.onError && typeof window[config.onError] === 'function') {
                window[config.onError](err);
            }
        });
    };

    session.oncancel = function () {
        config.log('Apple Pay session cancelled by user.');
    };

    session.begin();
};
