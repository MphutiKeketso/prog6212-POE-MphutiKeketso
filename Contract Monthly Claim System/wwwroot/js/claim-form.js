

// Claim form JavaScript functionality
$(document).ready(function () {
    initializeClaimForm();
    setupFileUpload();
    calculateTotalAmount();
});

function initializeClaimForm() {
    // Add first claim item if container is empty
    if ($('#claimItemsContainer').children().length === 0) {
        addClaimItem();
    }
    
    // Module change handler
    $(document).on('change', '.module-select', function() {
        var $row = $(this).closest('.claim-item-row');
        var moduleId = $(this).val();
        
        if (moduleId) {
            // Get module details via AJAX
            $.get('/api/modules/' + moduleId)
                .done(function(module) {
                    $row.find('.hourly-rate').val(module.hourlyRate);
                    calculateRowTotal($row);
                });
        }
    });
    
    // Hours worked change handler
    $(document).on('input', '.hours-worked', function() {
        var $row = $(this).closest('.claim-item-row');
        calculateRowTotal($row);
    });
    
    // Form validation
    $('#claimForm').on('submit', function(e) {
        if (!validateClaimForm()) {
            e.preventDefault();
        }
    });
}

function addClaimItem() {
    var itemCount = $('#claimItemsContainer .claim-item-row').length;
    var newItemHtml = `
        <div class="claim-item-row border rounded p-3 mb-3">
            <div class="row">
                <div class="col-md-3">
                    <label class="form-label">Programme *</label>
                    <select class="form-select programme-select" required>
                        <option value="">Select Programme</option>
                    </select>
                </div>
                <div class="col-md-3">
                    <label class="form-label">Module *</label>
                    <select class="form-select module-select" required disabled>
                        <option value="">Select Module</option>
                    </select>
                </div>
                <div class="col-md-2">
                    <label class="form-label">Hours Worked *</label>
                    <input type="number" class="form-control hours-worked" min="0" step="0.5" required>
                </div>
                <div class="col-md-2">
                    <label class="form-label">Hourly Rate</label>
                    <input type="number" class="form-control hourly-rate" readonly>
                </div>
                <div class="col-md-2">
                    <label class="form-label">Total Amount</label>
                    <div class="d-flex align-items-center">
                        <input type="text" class="form-control total-amount" readonly>
                        <button type="button" class="btn btn-outline-danger btn-sm ms-2 remove-item" 
                                onclick="removeClaimItem(this)">
                            <i class="fas fa-trash"></i>
                        </button>
                    </div>
                </div>
            </div>
            <div class="row mt-2">
                <div class="col-md-6">
                    <label class="form-label">Work Date *</label>
                    <input type="date" class="form-control work-date" required>
                </div>
                <div class="col-md-6">
                    <label class="form-label">Description</label>
                    <input type="text" class="form-control description" 
                           placeholder="Brief description of work performed">
                </div>
            </div>
        </div>
    `;
    
    $('#claimItemsContainer').append(newItemHtml);
    loadProgrammes($('#claimItemsContainer .claim-item-row').last().find('.programme-select'));
    
    // Update progress indicator
    updateProgressIndicator();
}

function removeClaimItem(button) {
    var $container = $('#claimItemsContainer');
    if ($container.children().length > 1) {
        $(button).closest('.claim-item-row').remove();
        calculateTotalAmount();
        updateProgressIndicator();
    } else {
        showAlert('At least one claim item is required.', 'warning');
    }
}

function calculateRowTotal($row) {
    var hours = parseFloat($row.find('.hours-worked').val()) || 0;
    var rate = parseFloat($row.find('.hourly-rate').val()) || 0;
    var total = hours * rate;
    
    $row.find('.total-amount').val(formatCurrency(total));
    calculateTotalAmount();
}

function calculateTotalAmount() {
    var total = 0;
    $('.claim-item-row').each(function() {
        var hours = parseFloat($(this).find('.hours-worked').val()) || 0;
        var rate = parseFloat($(this).find('.hourly-rate').val()) || 0;
        total += (hours * rate);
    });
    
    $('#totalClaimAmount').text(formatCurrency(total));
    
    // Update progress indicator if we have claim items
    if (total > 0) {
        updateProgressStep(1, true);
    }
}

function setupFileUpload() {
    // Drag and drop functionality
    var $uploadArea = $('.upload-area');
    
    $uploadArea.on('dragenter dragover', function(e) {
        e.preventDefault();
        $(this).addClass('drag-over');
    });
    
    $uploadArea.on('dragleave drop', function(e) {
        e.preventDefault();
        $(this).removeClass('drag-over');
        
        if (e.type === 'drop') {
            var files = e.originalEvent.dataTransfer.files;
            handleFileSelection(files);
        }
    });
    
    // File input change
    $('#documentsInput').on('change', function() {
        handleFileSelection(this.files);
    });
}

function handleFileSelection(files) {
    var $container = $('#uploadedFiles');
    
    Array.from(files).forEach(function(file) {
        if (validateFile(file)) {
            var fileHtml = `
                <div class="uploaded-file border rounded p-3 mb-2 d-flex justify-content-between align-items-center">
                    <div>
                        <i class="fas fa-file me-2"></i>
                        <strong>${file.name}</strong>
                        <small class="text-muted ms-2">(${formatFileSize(file.size)})</small>
                    </div>
                    <button type="button" class="btn btn-sm btn-outline-danger remove-file">
                        <i class="fas fa-trash"></i>
                    </button>
                </div>
            `;
            
            $container.append(fileHtml);
        }
    });
    
    // Update progress indicator if files are uploaded
    if ($container.children().length > 0) {
        updateProgressStep(2, true);
    }
}

function validateFile(file) {
    var allowedTypes = [
        'application/pdf',
        'application/msword',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
        'application/vnd.ms-excel',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
    ];
    
    var maxSize = 10 * 1024 * 1024; // 10MB
    
    if (!allowedTypes.includes(file.type)) {
        showAlert(`File type not supported: ${file.name}`, 'danger');
        return false;
    }
    
    if (file.size > maxSize) {
        showAlert(`File too large: ${file.name}. Maximum size is 10MB.`, 'danger');
        return false;
    }
    
    return true;
}

function updateProgressIndicator() {
    var hasBasicInfo = $('#claimMonth').val() && $('#submissionDate').val();
    var hasClaimItems = $('.claim-item-row').length > 0 && $('#totalClaimAmount').text() !== 'R0.00';
    var hasDocuments = $('#uploadedFiles').children().length > 0;
    
    updateProgressStep(0, hasBasicInfo);
    updateProgressStep(1, hasClaimItems);
    updateProgressStep(2, hasDocuments);
    updateProgressStep(3, hasBasicInfo && hasClaimItems); // Review step
}

function updateProgressStep(stepIndex, completed) {
    var $step = $('.progress-step').eq(stepIndex);
    
    if (completed) {
        $step.addClass('completed');
    } else {
        $step.removeClass('completed');
    }
}

function validateClaimForm() {
    var isValid = true;
    var errors = [];
    
    // Validate basic information
    if (!$('#claimMonth').val()) {
        errors.push('Claim month is required.');
        isValid = false;
    }
    
    if (!$('#submissionDate').val()) {
        errors.push('Submission date is required.');
        isValid = false;
    }
    
    // Validate claim items
    var hasValidItems = false;
    $('.claim-item-row').each(function() {
        var $row = $(this);
        var module = $row.find('.module-select').val();
        var hours = $row.find('.hours-worked').val();
        var workDate = $row.find('.work-date').val();
        
        if (module && hours && workDate) {
            hasValidItems = true;
        }
    });
    
    if (!hasValidItems) {
        errors.push('At least one valid claim item is required.');
        isValid = false;
    }
    
    // Show errors if any
    if (errors.length > 0) {
        showAlert(errors.join('<br>'), 'danger');
    }
    
    return isValid;
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    
    var k = 1024;
    var sizes = ['Bytes', 'KB', 'MB', 'GB'];
    var i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Event handlers
$(document).on('click', '.remove-file', function() {
    $(this).closest('.uploaded-file').remove();
    updateProgressIndicator();
});

$(document).on('change', '.programme-select', function() {
    var $row = $(this).closest('.claim-item-row');
    var $moduleSelect = $row.find('.module-select');
    var programmeId = $(this).val();
    
    $moduleSelect.prop('disabled', true).html('<option value="">Loading...</option>');
    
    if (programmeId) {
        $.get('/api/programmes/' + programmeId + '/modules')
            .done(function(modules) {
                var options = '<option value="">Select Module</option>';
                modules.forEach(function(module) {
                    options += `<option value="${module.moduleId}" data-rate="${module.hourlyRate}">
                                    ${module.moduleCode} - ${module.moduleName}
                                </option>`;
                });
                $moduleSelect.html(options).prop('disabled', false);
            })
            .fail(function() {
                $moduleSelect.html('<option value="">Error loading modules</option>');
                showAlert('Error loading modules. Please try again.', 'danger');
            });
    } else {
        $moduleSelect.html('<option value="">Select Module</option>').prop('disabled', true);
    }
});

function loadProgrammes($select) {
    $.get('/api/programmes')
        .done(function(programmes) {
            var options = '<option value="">Select Programme</option>';
            programmes.forEach(function(programme) {
                options += `<option value="${programme.programmeId}">
                                ${programme.programmeCode} - ${programme.programmeName}
                            </option>`;
            });
            $select.html(options);
        })
        .fail(function() {
            showAlert('Error loading programmes. Please refresh the page.', 'danger');
        });
}



$(document).ready(function () {
    // AUTOMATION: Auto-calculate totals instantly when inputs change
    $(document).on('input', '.hours-worked', function () {
        const $row = $(this).closest('.claim-item-row');
        const hours = parseFloat($(this).val()) || 0;
        const rate = parseFloat($row.find('.hourly-rate').val()) || 0;

        // Client-side calculation
        const total = (hours * rate).toFixed(2);
        $row.find('.total-amount').val('R ' + total); // Format as Currency

        updateGrandTotal();
    });

    // AUTOMATION: Fetch Hourly Rate automatically when module is selected
    $(document).on('change', '.module-select', function () {
        const moduleId = $(this).val();
        const $row = $(this).closest('.claim-item-row');

        if (moduleId) {
            // Use ASP.NET Web API to fetch rate
            $.get(`/api/claimsapi/module-rate/${moduleId}`)
                .done(function (data) {
                    if (data.success) {
                        $row.find('.hourly-rate').val(data.data);
                        $row.find('.hours-worked').trigger('input'); // Trigger calc
                    }
                });
        }
    });

    // VALIDATION: Strict checks before submission
    $('#claimForm').on('submit', function (e) {
        let isValid = true;
        let totalHours = 0;

        $('.hours-worked').each(function () {
            const h = parseFloat($(this).val()) || 0;
            totalHours += h;

            // Rule: Cannot work more than 24 hours in a single entry
            if (h > 24) {
                alert("Error: A single entry cannot exceed 24 hours.");
                isValid = false;
                $(this).addClass('is-invalid');
            }
        });

        // Rule: Policy Max Hours (e.g., 200 per month)
        if (totalHours > 200) {
            alert("Validation Failed: Total hours exceed the monthly limit of 200.");
            isValid = false;
        }

        if (!isValid) e.preventDefault();
    });
});

function updateGrandTotal() {
    let grandTotal = 0;
    $('.total-amount').each(function () {
        const val = parseFloat($(this).val().replace('R ', '')) || 0;
        grandTotal += val;
    });
    $('#totalClaimAmount').text('R ' + grandTotal.toFixed(2));
}

