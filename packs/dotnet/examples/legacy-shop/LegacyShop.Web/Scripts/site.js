// Tiny bit of 2013-style JavaScript: confirm before deleting in the admin grid.
document.addEventListener('click', function (e) {
    var t = e.target;
    if (t && t.tagName === 'A' && t.innerHTML === 'Delete') {
        if (!window.confirm('Delete this product? Its order history goes with it.')) {
            e.preventDefault();
        }
    }
});
