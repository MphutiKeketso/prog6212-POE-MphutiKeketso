

// Site-wide JavaScript functionality
$(document).ready(function () {
    // Initialize Bootstrap components
    initializeBootstrapComponents();
    
    // Setup CSRF token for AJAX requests
    setupCSRFToken();
    
    // Initialize common event handlers
    initializeCommonEventHandlers();
    
    // Setup notification system
    initializeNotificationSystem();
});

function initializeBootstrapComponents() {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
    
    // Initialize popovers
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
        return new bootstrap.Popover(popoverTriggerEl);
    });
}

function setupCSRFToken() {
    // Setup CSRF token for AJAX requests
    $.ajaxSetup({
        beforeSend: function (xhr, settings) {
            if (!csrfSafeMethod(settings.type) && !this.crossDomain) {
                xhr.setRequestHeader("RequestVerificationToken", 
                    $('input[name="__RequestVerificationToken"]').val());
            }
        }
    });
    
    function csrfSafeMethod(method) {
        return (/^(GET|HEAD|OPTIONS|TRACE)$/.test(method));
    }
}

function initializeCommonEventHandlers() {
    // Loading state for buttons
    $(document).on('click', '.btn-loading', function() {
        var $btn = $(this);
        var originalText = $btn.html();
        
        $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>Loading...')
            .prop('disabled', true);
        
        setTimeout(function() {
            $btn.html(originalText).prop('disabled', false);
        }, 2000);
    });
    
    // Confirm dialogs
    $(document).on('click', '[data-confirm]', function(e) {
        e.preventDefault();
        var message = $(this).data('confirm') || 'Are you sure?';
        
        if (confirm(message)) {
            if ($(this).is('form')) {
                $(this).submit();
            } else if ($(this).attr('href')) {
                window.location.href = $(this).attr('href');
            }
        }
    });
}

function initializeNotificationSystem() {
    // Check for new notifications every 30 seconds
    setInterval(function() {
        $.get('/api/notifications/unread-count')
            .done(function(count) {
                if (count > 0) {
                    $('#notification-badge').text(count).show();
                } else {
                    $('#notification-badge').hide();
                }
            });
    }, 30000);
}

// Utility functions
function showAlert(message, type = 'info', duration = 5000) {
    var alertHtml = `
        <div class="alert alert-${type} alert-dismissible fade show position-fixed" 
             style="top: 20px; right: 20px; z-index: 9999; min-width: 300px;">
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `;
    
    $('body').append(alertHtml);
    
    if (duration > 0) {
        setTimeout(function() {
            $('.alert').fadeOut();
        }, duration);
    }
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('en-ZA', {
        style: 'currency',
        currency: 'ZAR'
    }).format(amount);
}

function formatDate(date, format = 'short') {
    var options = {};
    
    switch (format) {
        case 'long':
            options = { year: 'numeric', month: 'long', day: 'numeric' };
            break;
        case 'short':
            options = { year: 'numeric', month: 'short', day: 'numeric' };
            break;
        case 'time':
            options = { hour: '2-digit', minute: '2-digit' };
            break;
        default:
            options = { year: 'numeric', month: 'short', day: 'numeric' };
    }
    
    return new Date(date).toLocaleDateString('en-ZA', options);
}
*/
