/* EasyCredit — helper global untuk halaman MVC.
   Berisi: token antiforgery, toast, confirm modal, dan utilitas kecil. */
window.EC = (function () {
    'use strict';

    var confirmResolver = null;

    function token() {
        return $('input[name="__RequestVerificationToken"]').first().val() || '';
    }

    function toast(type, message, title) {
        var isSuccess = type === 'success';
        var $toast = $('<div>')
            .addClass('app-toast ' + (isSuccess ? 'is-success' : 'is-error'))
            .attr('role', 'status');

        var $dot = $('<span>').addClass('toast-dot');
        var $body = $('<div>').addClass('toast-body');
        var $title = $('<strong>').text(title || (isSuccess ? 'Berhasil' : 'Gagal'));
        var $message = $('<span>').text(message || '');

        $body.append($title).append($message);
        $toast.append($dot).append($body);

        $('#toastStack').append($toast);

        setTimeout(function () {
            $toast.addClass('is-leaving');
            setTimeout(function () { $toast.remove(); }, 220);
        }, 4200);
    }

    function settleConfirm(value) {
        var resolver = confirmResolver;
        confirmResolver = null;

        if (resolver) {
            resolver(value);
        }
    }

    function confirmDialog(options) {
        options = options || {};

        return new Promise(function (resolve) {
            confirmResolver = resolve;

            $('#confirmTitle').text(options.title || 'Konfirmasi');
            $('#confirmMessage').text(options.message || 'Lanjutkan tindakan ini?');

            var $ok = $('#confirmOk')
                .text(options.confirmText || 'Ya, lanjutkan')
                .removeClass('btn-danger btn-accent');

            if (options.danger === false) {
                $ok.addClass('btn-accent');
            } else {
                $ok.addClass('btn-danger');
            }

            bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmModal')).show();
        });
    }

    function formatRupiah(value) {
        var number = Number(value || 0);
        return 'Rp ' + number.toLocaleString('id-ID');
    }

    $(function () {
        // Header antiforgery untuk request AJAX berbentuk JSON.
        // Request form-encoded tetap boleh mengirim __RequestVerificationToken di data.
        var currentToken = token();
        if (currentToken) {
            $.ajaxSetup({
                headers: { 'RequestVerificationToken': currentToken }
            });
        }

        // Auto-dismiss alert server-side.
        window.setTimeout(function () {
            $('[data-alert]').each(function () {
                bootstrap.Alert.getOrCreateInstance(this).close();
            });
        }, 6000);

        // Sidebar mobile.
        $('#sidebarToggle').on('click', function () {
            $('body').toggleClass('sidebar-open');
        });

        $('.sidebar').on('click', '.nav-link', function () {
            $('body').removeClass('sidebar-open');
        });

        // Resolusi confirm modal.
        $('#confirmOk').on('click', function () {
            bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmModal')).hide();
            settleConfirm(true);
        });

        $('#confirmModal').on('hidden.bs.modal', function () {
            settleConfirm(false);
        });

        $('#confirmModal').on('shown.bs.modal', function () {
            $('#confirmOk').trigger('focus');
        });
    });

    return {
        token: token,
        toast: toast,
        confirm: confirmDialog,
        formatRupiah: formatRupiah
    };
})();