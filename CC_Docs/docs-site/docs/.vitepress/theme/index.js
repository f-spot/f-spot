import { h } from 'vue'
import DefaultTheme from 'vitepress/theme'
import './custom.css'
import StatusBadge from './components/StatusBadge.vue'

export default {
  ...DefaultTheme,
  Layout: () => {
    return h(DefaultTheme.Layout, null, {
      // You can add custom slots here if needed
    })
  },
  enhanceApp({ app, router, siteData }) {
    // Register global components
    app.component('StatusBadge', StatusBadge)
  }
}