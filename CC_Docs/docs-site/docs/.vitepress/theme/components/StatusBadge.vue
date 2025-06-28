<template>
  <span 
    :class="[
      'status-badge',
      status,
      size
    ]"
  >
    <span v-if="icon" class="status-icon">{{ icon }}</span>
    <slot />
  </span>
</template>

<script setup>
defineProps({
  status: {
    type: String,
    default: 'info',
    validator: (value) => ['completed', 'in-progress', 'planned', 'critical', 'warning', 'info'].includes(value)
  },
  size: {
    type: String,
    default: 'normal',
    validator: (value) => ['small', 'normal', 'large'].includes(value)
  },
  icon: {
    type: String,
    default: ''
  }
})
</script>

<style scoped>
.status-badge {
  display: inline-flex;
  align-items: center;
  gap: 0.25rem;
  padding: 0.25rem 0.75rem;
  border-radius: 9999px;
  font-weight: 500;
  font-size: 0.875rem;
  white-space: nowrap;
}

.status-badge.small {
  padding: 0.125rem 0.5rem;
  font-size: 0.75rem;
}

.status-badge.large {
  padding: 0.375rem 1rem;
  font-size: 1rem;
}

.status-badge.completed {
  background-color: #dcfce7;
  color: #166534;
}

.status-badge.in-progress {
  background-color: #fef3c7;
  color: #92400e;
}

.status-badge.planned {
  background-color: #dbeafe;
  color: #1e40af;
}

.status-badge.critical {
  background-color: #fee2e2;
  color: #991b1b;
}

.status-badge.warning {
  background-color: #fef3c7;
  color: #92400e;
}

.status-badge.info {
  background-color: #eff6ff;
  color: #1e40af;
}

.dark .status-badge.completed {
  background-color: #166534;
  color: #dcfce7;
}

.dark .status-badge.in-progress {
  background-color: #92400e;
  color: #fef3c7;
}

.dark .status-badge.planned {
  background-color: #1e40af;
  color: #dbeafe;
}

.dark .status-badge.critical {
  background-color: #991b1b;
  color: #fee2e2;
}

.dark .status-badge.warning {
  background-color: #92400e;
  color: #fef3c7;
}

.dark .status-badge.info {
  background-color: #1e40af;
  color: #dbeafe;
}

.status-icon {
  font-size: 0.75em;
}
</style>