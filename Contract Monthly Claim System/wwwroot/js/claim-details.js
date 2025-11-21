

/ Claim details JavaScript functionality
$(document).ready(function () {
    initializeClaimDetails();
});

function initializeClaimDetails() {
    // Initialize document preview
    $('.document-preview').on('click', function (e) {
        e.preventDefault();
        var documentId = $(this).data('document-id');
        previewDocument(documentId);
    });
}

function showApprovalModal(action, claimId) {
    var modal = new bootstrap.Modal(document.getElementById('approvalModal'));
    var $title = $('#approvalModalTitle');
    var $button = $('#confirmApprovalBtn');
    var $action = $('#approvalAction');

    $action.val(action);

    if (action === 'approve') {
        $title.text('Approve Claim');
        $button.text('Confirm Approval')
            .removeClass('btn-danger')
            .addClass('btn-success');
    } else {
        $title.text('Reject Claim');
        $button.text('Confirm Rejection')
            .removeClass('btn-success')
            .addClass('btn-danger');
    }

    // Clear previous comments
    $('#approvalComments').val('');

    modal.show();
}

function downloadClaimPdf(claimId) {
    // Show loading state
    showAlert('Generating PDF...', 'info', 2000);

    // Create download link
    var downloadUrl = `/Claims/DownloadPdf/${claimId}`;
    window.open(downloadUrl, '_blank');
}

function previewDocument(documentId) {
    // Create modal for document preview
    var previewModal = `
        <div class="modal fade" id="documentPreviewModal" tabindex="-1">
            <div class="modal-dialog modal-lg">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title">Document Preview</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                    </div>
                    <div class="modal-body">
                        <div class="text-center">
                            <i class="fas fa-spinner fa-spin fa-3x"></i>
                            <p>Loading document...</p>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">Close</button>
                        <a href="/Documents/Download/${documentId}" class="btn btn-primary" target="_blank">
                            <i class="fas fa-download me-1"></i>Download
                        </a>
                    </div>
                </div>
            </div>
        </div>
    `;

    // Remove existing modal if any
    $('#documentPreviewModal').remove();
    $('body').append(previewModal);

    var modal = new bootstrap.Modal(document.getElementById('documentPreviewModal'));
    modal.show();

    // Load document preview (simplified - actual implementation would depend on document type)
    setTimeout(function () {
        $('.modal-body').html(`
            <div class="text-center">
                <i class="fas fa-file-pdf fa-4x text-danger mb-3"></i>
                <p>Document preview is not available for this file type.</p>
                <p>Please download the file to view its contents.</p>
            </div>
        `);
    }, 1000);
}

// Approval form submission
$('#approvalForm').on('submit', function (e) {
    e.preventDefault();

    var formData = {
        ClaimId: $('#claimId').val(),
        Action: $('#approvalAction').val(),
        Comments: $('#approvalComments').val(),
        NotifyLecturer: $('#notifyLecturer').is(':checked')
    };

    var $submitBtn = $('#confirmApprovalBtn');
    var originalText = $submitBtn.text();

    $submitBtn.html('<i class="fas fa-spinner fa-spin me-1"></i>Processing...')
        .prop('disabled', true);

    $.ajax({
        url: '/Claims/Approve',
        type: 'POST',
        data: formData,
        headers: {
            'RequestVerificationToken': $('input[name="__RequestVerificationToken"]').val()
        }
    })
        .done(function (response) {
            if (response.success) {
                showAlert(response.message, 'success');

                // Close modal
                bootstrap.Modal.getInstance(document.getElementById('approvalModal')).hide();

                // Reload page to show updated status
                setTimeout(function () {
                    window.location.reload();
                }, 1500);
            } else {
                showAlert(response.message, 'danger');
            }
        })
        .fail(function () {
            showAlert('An error occurred while processing the approval. Please try again.', 'danger');
        })
        .always(function () {
            $submitBtn.text(originalText).prop('disabled', false);
        });
});